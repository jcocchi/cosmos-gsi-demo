# Azure Cosmos DB Global Secondary Index Demo

Global secondary indexes (GSIs) help optimize cross partition queries in Azure Cosmos DB. GSIs are containers with a copy of data from a source container and are automatically kept in sync as data in the source container changes. Because GSIs are independent containers, they have their own partition key, throughput, indexing policy and any other container properties.

This project has two applications to demonstrate how to use global secondary indexes.
- A data loader project to write items into a source container.
- A console application to query both the source container and the global secondary index to compare RU and latency differences.

## Setup

### Configure your Azure Cosmos DB account

1. Create an Azure Cosmos DB for NoSQL account.

2. Enable continuous backups in the **Backups** page of your account.

3. Enable global secondary indexes in the **Features** page of your account.

4. Create a source container with the following configuration
    - Database name: UsersDB
    - Container name: users
    - Partition key: /email

> Tip: Global secondary indexes are effective at optimizing cross partition queries. The more physical partitions in a given container, the greater opportunity to reduce query RU charges and latency. You can guarantee a given number of physical partitions when creating a new container with autoscale throughput. To create a container with 10 physical partitions, create the container with 100,000 RUs. You can lower the throughput 10x after the container has been created.

5. Create a GSI container with the following configuration
    - Container name: usersByPhone
    - Source container name: users
    - GSI definition: SELECT * FROM c
    - Partition key: /phone/number

### Update Settings

1. In **appsettings.json** at the root of this repository, enter the endpoint of your Azure Cosmos DB account in **CosmosEndpoint**. The app settings are shared for both projects.

## Run the sample

### Load sample users

Run the **data-loader** project to populate the *users* container with 200,000 users. Notice that data is automatically synced to the *usersByPhone* container.

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

> Tip: Because the data is randomly generated, you may need to swap out the phone number and area code that is used in the sample queries. If no items are returned, try replacing these values in **query-gsi/Program.cs** on lines 28 and 37 with values from your generated dataset.