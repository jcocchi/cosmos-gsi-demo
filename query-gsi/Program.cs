using query_gsi;
using Azure.Identity;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

string ordersDBName = config["OrdersDatabase"];
string ordersName = config["OrdersSource"];
string ordersGSIName = config["OrdersGSI"];

CosmosClientOptions clientOptions = new CosmosClientOptions()
{
    UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    }
};
List<(string, string)> containersToInitialize = new List<(string, string)> 
    {
        (ordersDBName, ordersName),
        (ordersDBName, ordersGSIName)
    };
CosmosClient client = await CosmosClient.CreateAndInitializeAsync(config["CosmosEndpoint"], new DefaultAzureCredential(), containersToInitialize, clientOptions);
Container ordersSource = client.GetDatabase(ordersDBName).GetContainer(ordersName);
Container ordersGSI = client.GetDatabase(ordersDBName).GetContainer(ordersGSIName);

Console.WriteLine($"Hello, welcome to the Azure Cosmos DB GSI demo!");
Console.WriteLine($"----------------------------------------------------------- \n\n");

await RunOrdersDemo(ordersSource, ordersGSI);

static async Task RunOrdersDemo(Container source, Container gsi)
{
    Console.WriteLine("Finding orders by customerId.");
    Console.WriteLine($"-----------------------------------------------------------");

    var findByCustomerId = "SELECT * FROM c WHERE c.customerId = \"933d0d0d-0b83-4ced-b6da-b8d2d12370ad\"";
    var statsSource_Cust = await RunQuery(source, findByCustomerId, false);

    PrintComparisonOutput(statsSource_Cust, null, findByCustomerId);

    Console.WriteLine("Finding orders by zip.");
    Console.WriteLine($"-----------------------------------------------------------");

    var findOrderByZip = "SELECT * FROM c WHERE c.shippingAddress.zipCode = \"63756\"";
    var statsSource_Zip = await RunQuery(source, findOrderByZip, false);
    var statsGSI_Zip = await RunQuery(gsi, findOrderByZip, false);

    PrintComparisonOutput(statsSource_Zip, statsGSI_Zip, findOrderByZip);

    var findOrderByAddress = "SELECT * FROM c WHERE c.shippingAddress.zipCode = \"63756\" and c.shippingAddress.street = \"Parisian Rapids\"";
    var statsSource = await RunQuery(source, findOrderByAddress, false);
    var statsGSI = await RunQuery(gsi, findOrderByAddress, false);

    PrintComparisonOutput(statsSource, statsGSI, findOrderByAddress);
}

static async Task<QueryStats> RunQuery(Container container, string queryText, bool printMetrics)
{
    var query = new QueryDefinition(queryText);

    Console.WriteLine($"Running against container {container.Id}.");
    Console.WriteLine($"\t* Query: {queryText}\n");

    var requestCharge = 0.0;
    var executionTime = new TimeSpan();
    var results = new List<dynamic>();

    List<ServerSideCumulativeMetrics> metrics = new List<ServerSideCumulativeMetrics>();
    var resultSetIterator = container.GetItemQueryIterator<dynamic>(query, null, new QueryRequestOptions() { PopulateIndexMetrics = true});
    while (resultSetIterator.HasMoreResults)
    {
        var response = await resultSetIterator.ReadNextAsync();
        results.AddRange(response.Resource);
        requestCharge += response.RequestCharge;
        executionTime += response.Diagnostics.GetClientElapsedTime();

        var tripMetrics = response.Diagnostics.GetQueryMetrics();
        if (tripMetrics != null) 
            metrics.Add(tripMetrics);

        Console.WriteLine($"Trip num items: {response.Count}, Trip request charge: {response.RequestCharge}, Trip execution time: {response.Diagnostics.GetClientElapsedTime()}");
    }

    if(printMetrics)
    {
        TimeSpan docLoadTime = metrics.Aggregate(TimeSpan.Zero, (currentSum, next) => currentSum + next.CumulativeMetrics.DocumentLoadTime);
        TimeSpan docWriteTime = metrics.Aggregate(TimeSpan.Zero, (currentSum, next) => currentSum + next.CumulativeMetrics.DocumentWriteTime);
        TimeSpan indexLookupTime = metrics.Aggregate(TimeSpan.Zero, (currentSum, next) => currentSum + next.CumulativeMetrics.IndexLookupTime);
        TimeSpan queryPrepTime = metrics.Aggregate(TimeSpan.Zero, (currentSum, next) => currentSum + next.CumulativeMetrics.QueryPreparationTime);
        TimeSpan runtimeExecutionTime = metrics.Aggregate(TimeSpan.Zero, (currentSum, next) => currentSum + next.CumulativeMetrics.RuntimeExecutionTime);

        Console.WriteLine("Query execution time breakdown across trips");
        Console.WriteLine($"\tDocument Load Time: {docLoadTime}");
        Console.WriteLine($"\tDocument Write Time: {docWriteTime}");
        Console.WriteLine($"\tIndex Lookup Time: {indexLookupTime}");
        Console.WriteLine($"\tQuery Preparation Time: {queryPrepTime}");
        Console.WriteLine($"\tRuntime Execution Time: {runtimeExecutionTime}\n");
    }

    Console.WriteLine($"Final Request charge: {requestCharge}, Final execution time: {executionTime}, Total items: {results.Count}\n\n");

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
    if(gsiStats != null)
        Console.WriteLine("|GSI container    |{0, -10}|{1, -16}|", Math.Round(gsiStats.RUCharge, 2), gsiStats.ExecutionTime);

    Console.WriteLine("Press enter to continue...");
    Console.ReadLine();
    Console.WriteLine();
}
