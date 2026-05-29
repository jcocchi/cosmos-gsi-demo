# Azure Cosmos DB Global Secondary Index Demo

Global secondary indexes (GSIs) help optimize cross partition queries in Azure Cosmos DB. GSIs are containers with a copy of data from a source container and are automatically kept in sync as data in the source container changes. Because GSIs are independent containers, they have their own partition key, throughput, indexing policy as well as any other container properties.

This project has two applications to demonstrate how to use global secondary indexes.
- A data loader project to write items into a source container.
- A console application to query both the source container and the global secondary index to compare RU and latency differences.

## Setup

### Configure your Azure Cosmos DB account

1. Create an Azure Cosmos DB for NoSQL account.

2. Enable continuous backups in the **Backups** page of your account.

3. Enable global secondary indexes in the **Features** page of your account.

4. Create a source container with the following configuration
    - Database name: OrdersDB
    - Container name: orders
    - Partition key: /customerId

> Tip: Global secondary indexes are effective at optimizing cross partition queries. The more physical partitions in a given container, the greater opportunity to reduce query RU charges and latency. You can guarantee a given number of physical partitions when creating a new container with autoscale throughput. To create a container with 10 physical partitions, create the container with 100,000 RUs. You can lower the throughput 10x after the container has been created.

5. Create a GSI container with the following configuration
    - Container name: ordersByZip
    - Source container name: orders
    - GSI definition: SELECT c.id, c.customerId, c.tenantId, c.orderDate, c.orderStatus, c.totalAmount, c.payment, c.shippingAddress FROM c
    - Partition key: /shippingAddress/zipCode, /shippingAddress/street

6. This sample uses identity-based authentication (Microsoft Entra ID / RBAC). Make sure your account has the **Cosmos DB Built-in Data Contributor** role assigned.

### Update Settings

1. In **appsettings.json** at the root of this repository, enter the endpoint of your Azure Cosmos DB account in **CosmosEndpoint**. The app settings are shared for both projects.

## Run the sample

### Load sample orders

Run the **data-loader** project to populate the *orders* container with 200,000 orders. Notice that data is automatically synced to the *ordersByZip* container.

```cmd
cd data-loader
dotnet run
```

### Run the sample query project

Run the **query-gsi** project to test out the same queries executed on both the source and GSI containers. There will be console output for each round trip in the query as well as an execution summary comparing the RU charge and latency. 

```cmd
cd query-gsi
dotnet run
```

> Tip: Because the data is randomly generated, you may need to swap out the customer id, zip code and street values used in the sample queries. If no items are returned, try replacing these values in **query-gsi/Program.cs** with values from your generated dataset.