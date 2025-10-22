 using Microsoft.Azure.Cosmos;

 public class TxnRollbackDemo
{
    public static async Task Main()
    {
        string connectionString = Environment.GetEnvironmentVariable("COSMOSDB_CONNECTION") 
            ?? throw new InvalidOperationException("COSMOSDB_CONNECTION not set");

        CosmosClient client = new CosmosClient(connectionString);
        Container container = client.GetContainer("OrdersDB", "Orders");
        string pk = "TxnDemoPK";
        string secondId = Guid.NewGuid().ToString();

        var existing = new { id = "fixed-id", orderId = pk, content = "first insert" };
        await container.CreateItemAsync(existing, new PartitionKey(pk));

        TransactionalBatch batch = container.CreateTransactionalBatch(new PartitionKey(pk))
            .CreateItem(existing) // duplicate id -> causes Conflict
            .CreateItem(new { id = secondId, orderId = pk, content = "second insert" });

        TransactionalBatchResponse resp = await batch.ExecuteAsync();
        Console.WriteLine($"Success: {resp.IsSuccessStatusCode}, Status: {resp.StatusCode}");

// Verify rollback (second item should NOT exist)
        try
        {
            await container.ReadItemAsync<dynamic>(secondId, new PartitionKey(pk));
            Console.WriteLine("Unexpected: second item exists.");
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine("✅ Verified rollback: second item not found.");
        }

    }
}
