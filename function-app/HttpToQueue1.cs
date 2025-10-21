using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionApp
{
    public class HttpToQueue1
    {
        private readonly ILogger _logger;

        public HttpToQueue1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpToQueue1>();
        }

        // Just accepts a request and logs the body
        [Function("HttpToQueue1")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequestData req)
        {
            var body = await req.ReadAsStringAsync();
            _logger.LogInformation("HttpToQueue1 Received: {Body}", body);

            var res = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await res.WriteStringAsync("OK");
            return res;
        }
    }
}
