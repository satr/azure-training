using Azure.Identity;
using Microsoft.Azure.Cosmos;
using cosmos_db_app;
using Container = Microsoft.Azure.Cosmos.Container; // only for console formatting

const string databaseName = "OrdersDB";
const string containerName = "Orders";     // assumes partition key path is /orderId

// ---------- Build a CosmosClient ----------
CosmosClient client = BuildClient();

// Get (or create for demo) DB + container:
Database db = await client.CreateDatabaseIfNotExistsAsync(databaseName);
Container container = await db.CreateContainerIfNotExistsAsync(
    id: containerName,
    partitionKeyPath: "/orderId",
    throughput: 400); // for demo; omit if pre-provisioned

// ---------- Create ----------
var newOrder = new Order
{
    id = Guid.NewGuid().ToString(),
    orderId = DateTime.UtcNow.Ticks.ToString(),   // PK must be string if PK path is /orderId
    content = "hello from CLI",
    processedAt = DateTime.UtcNow
};

ItemResponse<Order> createResp =
    await container.CreateItemAsync(newOrder, new PartitionKey(newOrder.orderId));
Console.WriteLine($"Created id={createResp.Resource.id} RU={createResp.RequestCharge}");

// ---------- Read (point read) ----------
ItemResponse<Order> readResp =
    await container.ReadItemAsync<Order>(newOrder.id, new PartitionKey(newOrder.orderId));
Console.WriteLine($"Read content={readResp.Resource.content} RU={readResp.RequestCharge}");

// ---------- Query (SQL) ----------
var q = new QueryDefinition(
    "SELECT * FROM c WHERE c.orderId = @pk")
    .WithParameter("@pk", newOrder.orderId);

using FeedIterator<Order> feed = container.GetItemQueryIterator<Order>(q, requestOptions:
    new QueryRequestOptions { PartitionKey = new PartitionKey(newOrder.orderId), MaxItemCount = 50 });

while (feed.HasMoreResults)
{
    FeedResponse<Order> page = await feed.ReadNextAsync();
    foreach (var o in page)
    {
        Console.WriteLine($"Queried id={o.id} content={o.content}");
    }
    Console.WriteLine($"Query page RU={page.RequestCharge}");
}

// ---------- Replace (full doc update) ----------
newOrder.content = "updated content";
ItemResponse<Order> replaceResp =
    await container.ReplaceItemAsync(newOrder, newOrder.id, new PartitionKey(newOrder.orderId));
Console.WriteLine($"Replaced RU={replaceResp.RequestCharge}");

// ---------- Patch (partial) ----------
ItemResponse<Order> patchResp =
    await container.PatchItemAsync<Order>(
        id: newOrder.id,
        partitionKey: new PartitionKey(newOrder.orderId),
        patchOperations: new[]
        {
            PatchOperation.Replace("/content", "patched content"),
            PatchOperation.Add("/patchedAt", DateTime.UtcNow)
        });
Console.WriteLine($"Patched RU={patchResp.RequestCharge}");

// ---------- Upsert ----------
newOrder.content = "upserted content";
ItemResponse<Order> upsertResp =
    await container.UpsertItemAsync(newOrder, new PartitionKey(newOrder.orderId));
Console.WriteLine($"Upsert RU={upsertResp.RequestCharge}");

// ---------- Delete ----------
ItemResponse<Order> deleteResp =
    await container.DeleteItemAsync<Order>(newOrder.id, new PartitionKey(newOrder.orderId));
Console.WriteLine($"Deleted RU={deleteResp.RequestCharge}");

// ---------- Helpers ----------
static CosmosClient BuildClient()
{
    // Prefer connection string if present
    var conn = Environment.GetEnvironmentVariable("COSMOSDB_CONNECTION");
    if (!string.IsNullOrWhiteSpace(conn))
    {
        return new CosmosClient(conn, new CosmosClientOptions
        {
            // Useful options:
            ApplicationName = "CosmosCliDemo",
            AllowBulkExecution = false,
            // Use camelCase serialization to match typical JSON shapes
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        });
    }

    // Otherwise use AAD (DefaultAzureCredential) with endpoint
    var endpoint = Environment.GetEnvironmentVariable("COSMOSDB_ENDPOINT")
        ?? throw new InvalidOperationException("Set COSMOSDB_CONNECTION or COSMOSDB_ENDPOINT.");

    // Requires: dotnet add package Azure.Identity
    var credential = new DefaultAzureCredential();
    return new CosmosClient(endpoint, credential, new CosmosClientOptions
    {
        ApplicationName = "CosmosCliDemo",
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    });
}
