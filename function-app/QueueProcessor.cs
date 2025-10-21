using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using System.Threading.Tasks;

namespace MyFuncApp.Functions;

public class QueueProcessor
{
    private readonly ILogger<QueueProcessor> _logger;
    public QueueProcessor(ILogger<QueueProcessor> logger) => _logger = logger;

    // The returned string is written to the blob path below
    [Function("QueueProcessor")]
    [BlobOutput("processed/{rand-guid}.txt", Connection = "AzureWebJobsStorage")]
    public string Run(
        [ServiceBusTrigger("orders", Connection = "ServiceBusConnection")] string message)
    {
        _logger.LogInformation("QueueProcessor received: {Message}", message);
        var processed = $"Processed: {message}";
        return processed; // <-- becomes the blob content
    }
}