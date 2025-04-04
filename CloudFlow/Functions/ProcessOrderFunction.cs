using Application.Models;
using Confluent.Kafka;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzzurFunctionApp.Functions
{
    public class ProcessOrderFunction
    {
        private readonly ILogger<ProcessOrderFunction> _logger;

        public ProcessOrderFunction(ILogger<ProcessOrderFunction> logger)
        {
            _logger = logger;
        }

        [Function("ProcessOrder")]
        public async Task Run([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("Timer triggered - checking Kafka...");

            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "order-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe("orders");

            var cr = consumer.Consume(TimeSpan.FromSeconds(5));
            if (cr != null)
            {
                var order = JsonConvert.DeserializeObject<Order>(cr.Message.Value);
                _logger.LogInformation($"Order consumed: {order.ProductName}");
            }

            consumer.Close();
        }
    }
}
