using System.Text.Json;
using Bogus;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using data_loader;
using System.Diagnostics;

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
Container source = client.GetDatabase(config["OrdersDatabase"]).GetContainer(config["OrdersSource"]);

var faker = new Faker("en")
{
    Random = new Randomizer(42) // Seed value
};

var numDocsToWrite = 200000;
var batchSize = 50;
var sleep = 1000;

var numWritten = 0;

Console.WriteLine($"Welcome to the Cosmos DB Bulk Loader. \n\nWriting {numDocsToWrite} records...");
while (numWritten < numDocsToWrite)
{
    Console.WriteLine($"Writing {batchSize} more records... total written so far {numWritten}");
    var cost = 0.0;
    var errors = 0;

    Stopwatch stopWatch = new Stopwatch();
    stopWatch.Start();

    List<Task> concurrentTasks = new List<Task>();
    var orders = GenerateRandomOrders(batchSize);

    foreach (var order in orders)
    {
        concurrentTasks.Add(source.CreateItemAsync(order, new PartitionKey(order.CustomerId)).ContinueWith(t =>
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

    Thread.Sleep(sleep);
    stopWatch.Stop();

    Console.WriteLine($"Documents written this batch:{batchSize - errors}   Cost: {cost}   Errors: {errors}   Time: {stopWatch.Elapsed} \n");
}

Console.WriteLine($"Finished writing {numWritten} records.");

static List<Order> GenerateRandomOrders(int numberOfDocumentsPerBatch)
{
    var productFaker = new Faker<Product>()
        .StrictMode(true)
        .RuleFor(p => p.ProductId, f => Guid.NewGuid().ToString())
        .RuleFor(p => p.ProductName, f => f.Commerce.ProductName())
        .RuleFor(p => p.Quantity, f => f.Random.Int(1, 5))
        .RuleFor(p => p.Price, f => f.Finance.Amount());

    var paymentInfoFaker = new Faker<PaymentInfo>()
        .StrictMode(true)
        .RuleFor(p => p.TransactionId, f => Guid.NewGuid().ToString())
        .RuleFor(p => p.Paid, f => f.Random.Bool());

    var addressFaker = new Faker<Address>()
        .StrictMode(true)
        .RuleFor(a => a.Street, f => f.Address.StreetName())
        .RuleFor(a => a.City, f => f.Address.City())
        .RuleFor(a => a.State, f => f.Address.State())
        .RuleFor(a => a.ZipCode, f => f.Address.ZipCode())
        .RuleFor(a => a.Country, f => f.Address.Country());

    var orderStatuses = new[] { "New", "Processing", "Shipped", "Delivered", "Cancelled" };
    var weights = new[] { .8f,  };
    var orderFaker = new Faker<Order>()
        .StrictMode(true)
        .RuleFor(o => o.Id, f => Guid.NewGuid().ToString())
        .RuleFor(o => o.CustomerId, f => Guid.NewGuid().ToString())
        .RuleFor(o => o.TenantId, f => $"tenant-{f.Random.Int(1, 10)}")
        .RuleFor(o => o.Products, f => productFaker.Generate(f.Random.Int(100, 100)))
        .RuleFor(t => t.OrderDate, f => f.Date.Past(5).ToUniversalTime())
        .RuleFor(o => o.OrderStatus, f => f.PickRandom(orderStatuses))
        .RuleFor(o => o.TotalAmount, f => f.Finance.Amount())
        .RuleFor(o => o.Payment, f => paymentInfoFaker.Generate())
        .RuleFor(o => o.ShippingAddress, f => addressFaker.Generate());

    var orders = orderFaker.Generate(numberOfDocumentsPerBatch, null);

    return orders;
}