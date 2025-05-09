using Azure.Identity;
using Microsoft.Azure.Cosmos;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using query_gsi;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

CosmosClientOptions clientOptions = new CosmosClientOptions()
{
    UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    }
};
CosmosClient client = new CosmosClient(config["CosmosEndpoint"], new DefaultAzureCredential(), clientOptions);
Container source = client.GetDatabase(config["DatabaseName"]).GetContainer(config["SourceContainerName"]);
Container gsi = client.GetDatabase(config["DatabaseName"]).GetContainer(config["GSIName"]);

Console.WriteLine($"Hello, welcome to the Azure Cosmos DB GSI demo!");
Console.WriteLine($"----------------------------------------------------------- \n\n");

Console.WriteLine("Finding users by phone number.");
Console.WriteLine($"-----------------------------------------------------------");

var findByPhoneText = "SELECT * FROM c WHERE c.phone.number = \"852-864-8015\"";
var statsSource = await RunQuery(source, findByPhoneText);
var statsGSI = await RunQuery(gsi, findByPhoneText);

PrintComparisonOutput(statsSource, statsGSI, findByPhoneText);

Console.WriteLine("Finding active users by area code.");
Console.WriteLine($"-----------------------------------------------------------");

var findActiveByAreaText = "SELECT * FROM c WHERE STARTSWITH(c.phone.number, \"507\") and c.isActive = true";
statsSource = await RunQuery(source, findActiveByAreaText);
statsGSI = await RunQuery(gsi, findActiveByAreaText);

PrintComparisonOutput(statsSource, statsGSI, findActiveByAreaText);

static async Task<QueryStats> RunQuery(Container container, string queryText)
{
    var query = new QueryDefinition(queryText);

    Console.WriteLine($"Running against container {container.Id}.");
    Console.WriteLine($"\t* Query: {queryText}\n");

    var requestCharge = 0.0;
    var executionTime = new TimeSpan();
    var results = new List<dynamic>();

    var resultSetIterator = container.GetItemQueryIterator<dynamic>(query, null, null);
    while (resultSetIterator.HasMoreResults)
    {
        var response = await resultSetIterator.ReadNextAsync();
        results.AddRange(response.Resource);
        requestCharge += response.RequestCharge;
        executionTime += response.Diagnostics.GetClientElapsedTime();

        Console.WriteLine($"Trip num items: {response.Count}, Trip request charge: {response.RequestCharge}, Trip execution time: {response.Diagnostics.GetClientElapsedTime()}");
    }

    Console.WriteLine($"Final Request charge: {requestCharge}, Final execution time: {executionTime}\n\n");

    var stats = new QueryStats()
    {
        RUCharge = requestCharge,
        ExecutionTime = executionTime
    };

    return stats;
}

static void PrintComparisonOutput(QueryStats sourceStats, QueryStats gsiStats, string queryText)
{
    Console.WriteLine($"\nShowing final results for query \"{queryText}\"");
    Console.WriteLine($"-----------------------------------------------------------");

    Console.WriteLine("|Setup            |RU Charge |Execution Time  |");
    Console.WriteLine("|-----------------|----------|----------------|");
    Console.WriteLine("|Source container |{0, -10}|{1, -16}|", Math.Round(sourceStats.RUCharge, 2), sourceStats.ExecutionTime);
    Console.WriteLine("|GSI container    |{0, -10}|{1, -16}|", Math.Round(gsiStats.RUCharge, 2), gsiStats.ExecutionTime);

    Console.WriteLine("Press enter to continue...");
    Console.ReadLine();
    Console.WriteLine();
}