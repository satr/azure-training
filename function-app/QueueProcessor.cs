using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class QueueProcessor
{
    private readonly ILogger<QueueProcessor> _logger;
    public QueueProcessor(ILogger<QueueProcessor> logger) => _logger = logger;

    [Function("QueueProcessor")]
    [CosmosDBOutput(
        databaseName: "OrdersDB",
        containerName: "Orders",
        Connection = "CosmosDBConnection")]
    public object Run(
        [ServiceBusTrigger("orders", Connection = "ServiceBusConnection")] string message)
    {
        _logger.LogInformation("QueueProcessor received5: {Message}", message);

        try
        {
            return new
            {
                id = Guid.NewGuid().ToString(),
                orderId = DateTime.UtcNow.Ticks.ToString(),
                content = message,
                processedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to return bound db-data");
        }
        return null;
    }
}