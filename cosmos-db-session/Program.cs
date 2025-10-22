using Microsoft.Azure.Cosmos;

// Assumes env var COSMOSDB_CONNECTION is set to a valid connection string
// DB: OrdersDB, Container: Orders, PK path: /orderId (string)

public class Program
{
    private const string DbName = "OrdersDB";
    private const string ContainerName = "Orders";

    public static async Task Main()
    {
        string conn = Environment.GetEnvironmentVariable("COSMOSDB_CONNECTION")
            ?? throw new InvalidOperationException("Set COSMOSDB_CONNECTION.");

        // Two separate clients (separate sessions)
        var clientA = new CosmosClient(conn, new CosmosClientOptions
        {
            ConsistencyLevel = ConsistencyLevel.Session,
            ApplicationName = "SessionDemo-Writer"
        });
        var clientB = new CosmosClient(conn, new CosmosClientOptions
        {
            ConsistencyLevel = ConsistencyLevel.Session,
            ApplicationName = "SessionDemo-Reader"
        });

        var containerA = clientA.GetContainer(DbName, ContainerName);
        var containerB = clientB.GetContainer(DbName, ContainerName);

        // the partition key value we’ll use
        string pk = "SessionDemoPK";
        string id = Guid.NewGuid().ToString();

        var doc = new
        {
            id,
            orderId = pk,                 // PK must be a string for /orderId
            content = "session test",
            createdUtc = DateTime.UtcNow
        };

        // 1) client A writes and we capture the session token
        ItemResponse<dynamic> writeResp =
            await containerA.CreateItemAsync<dynamic>(doc, new PartitionKey(pk));

        string sessionToken = writeResp.Headers.Session;
        Console.WriteLine($"Write RU: {writeResp.RequestCharge:F2}");
        Console.WriteLine($"Session token (A): {sessionToken}");

        // 2) client B tries to read immediately WITHOUT the token
        try
        {
            var readNoToken =
                await containerB.ReadItemAsync<dynamic>(id, new PartitionKey(pk));
            Console.WriteLine("Reader (B) WITHOUT token read the doc:");
            Console.WriteLine($"  content={readNoToken.Resource.content}");
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine("Reader (B) WITHOUT token did NOT see the doc (404).");
        }

        // 3) client B reads WITH the session token from client A (guaranteed to see the write)
        var requestOptions = new ItemRequestOptions { SessionToken = sessionToken };
        var readWithToken =
            await containerB.ReadItemAsync<dynamic>(id, new PartitionKey(pk), requestOptions);

        Console.WriteLine("Reader (B) WITH token read the doc:");
        Console.WriteLine($"  content={readWithToken.Resource.content}");
        Console.WriteLine($"Read RU with token: {readWithToken.RequestCharge:F2}");
    }
}
