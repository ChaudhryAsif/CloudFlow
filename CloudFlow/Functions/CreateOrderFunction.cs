using Application.Commands;
using Application.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzzurFunctionApp.Functions
{
    public class CreateOrderFunction
    {
        private readonly OrderCommandHandler _orderCommandHandler;
        private readonly ILogger<CreateOrderFunction> _logger;

        public CreateOrderFunction(OrderCommandHandler orderCommandHandler, ILogger<CreateOrderFunction> logger)
        {
            _orderCommandHandler = orderCommandHandler;
            _logger = logger;
        }

        [Function("CreateOrder")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();
            var order = JsonConvert.DeserializeObject<Order>(body);

            await _orderCommandHandler.PublishOrderAsync(order);
            _logger.LogInformation("Order Published Successfully");

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("Order Published to Kafka!");
            return response;
        }
    }
}
