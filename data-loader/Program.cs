using System.Text.Json;
using Bogus;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using data_loader;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

CosmosClientOptions clientOptions = new CosmosClientOptions()
{
    UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    },
    AllowBulkExecution = true
};
CosmosClient client = new CosmosClient(config["CosmosEndpoint"], new DefaultAzureCredential(), clientOptions);
Container source = client.GetDatabase(config["DatabaseName"]).GetContainer(config["SourceContainerName"]);

var faker = new Faker("en")
{
    Random = new Randomizer(42) // Seed value
};

var numDocsToWrite = 200000;
var batchSize = 100;
var sleep = 1000;

var numWritten = 0;

Console.WriteLine($"Welcome to the Cosmos DB Bulk Loader. \n\nWriting {numDocsToWrite} records...");
while (numWritten < numDocsToWrite)
{
    Console.WriteLine($"Writing {batchSize} more records... total written so far {numWritten}");
    var cost = 0.0;
    var errors = 0;

    List<Task> concurrentTasks = new List<Task>();
    var users = GenerateRandomUsers(batchSize);
    foreach (var user in users)
    {
        concurrentTasks.Add(source.CreateItemAsync(user, new PartitionKey(user.Email)).ContinueWith(t =>
        {
            if (t.Status == TaskStatus.RanToCompletion)
            {
                cost += t.Result.RequestCharge;
            }
            else
            {
                Console.WriteLine($"Error creating document: {t.Exception.Message}");
                errors++;
            }
        }));
    }

    await Task.WhenAll(concurrentTasks);
    numWritten += batchSize - errors;
    Console.WriteLine($"Documents written this batch:{batchSize - errors}   Cost: {cost}   Errors: {errors} \n");

    Thread.Sleep(sleep);
}

Console.WriteLine($"Finished writing {numWritten} records.");

static List<data_loader.User> GenerateRandomUsers(int numberOfDocumentsPerBatch)
{
    var nameFaker = new Faker<Name>()
        .StrictMode(true)
        .RuleFor(n => n.First, f => f.Name.FirstName())
        .RuleFor(n => n.Last, f => f.Name.LastName());

    var phoneTypes = new[] { "Mobile", "Home", "Work", "Other" };
    var phoneFaker = new Faker<Phone>()
        .StrictMode(true)
        .RuleFor(p => p.Number, f => f.Phone.PhoneNumberFormat())
        .RuleFor(p => p.Type, f => f.PickRandom(phoneTypes));

    var userFaker = new Faker<data_loader.User>()
        .StrictMode(true)
        .RuleFor(u => u.Id, f => f.Internet.UserName())
        .RuleFor(u => u.Name, f => nameFaker.Generate())
        .RuleFor(u => u.Email, f => f.Internet.Email())
        .RuleFor(u => u.SecondaryEmails, f => Enumerable.Range(1, 3)
                            .Select(_ => f.Internet.Email())
                            .ToList())
        .RuleFor(u => u.Phone, f => phoneFaker.Generate())
        .RuleFor(u => u.SecondaryPhones, f => phoneFaker.Generate(f.Random.Int(1, 3)))
        .RuleFor(u => u.DateOfBirth, f => f.Person.DateOfBirth.ToUniversalTime())
        .RuleFor(u => u.IsActive, f => f.Random.Bool());

    var users = userFaker.Generate(numberOfDocumentsPerBatch, null);

    return users;
}