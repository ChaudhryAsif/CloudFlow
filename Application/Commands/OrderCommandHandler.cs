using Application.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Commands
{
    public class OrderCommandHandler
    {
        private readonly ILogger<OrderCommandHandler> _logger;
        private const string KafkaTopic = "orders";
        private readonly string _bootstrapServers = "localhost:9092";

        public OrderCommandHandler(ILogger<OrderCommandHandler> logger)
        {
            _logger = logger;
        }

        public async Task PublishOrderAsync(Order order)
        {
            var config = new ProducerConfig { BootstrapServers = _bootstrapServers };
            using var producer = new ProducerBuilder<Null, string>(config).Build();

            var message = new Message<Null, string>
            {
                Value = JsonSerializer.Serialize(order)
            };

            await producer.ProduceAsync(KafkaTopic, message);
            _logger.LogInformation($"Order Published: {order.Id}");
        }
    }
}
