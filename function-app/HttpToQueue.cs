using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;

public class HttpToQueue
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger _logger;

    public HttpToQueue(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<HttpToQueue>();
        _client = new ServiceBusClient(System.Environment.GetEnvironmentVariable("ServiceBusConnection"));
        _sender = _client.CreateSender("orders");
    }

    [Function("HttpToQueue")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        string message = await req.ReadAsStringAsync();
        await _sender.SendMessageAsync(new ServiceBusMessage(message));
        _logger.LogInformation($"Sent message: {message}");
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync("Message queued!");
        return response;
    }
}
