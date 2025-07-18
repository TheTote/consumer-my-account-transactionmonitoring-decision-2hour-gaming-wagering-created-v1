using System.Text.Json;
using Confluent.Kafka;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1;

public interface IKafkaJsonProducer<TKey>
{
    Task<DeliveryResult<TKey, string>> ProduceAsync<TValue>(string topicName, TKey key, TValue value, JsonSerializerOptions options = null, CancellationToken cancellationToken = default);
}

public class KafkaJsonProducer<TKey>(ProducerConfig config) : IKafkaJsonProducer<TKey>
{
    private readonly IProducer<TKey, string> _producer = new ProducerBuilder<TKey, string>(config).Build();

    public async Task<DeliveryResult<TKey, string>> ProduceAsync<TValue>(string topicName, TKey key, TValue value, JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, options);
        return await _producer.ProduceAsync(
            topicName,
            new Message<TKey, string> { Key = key, Value = json },
            cancellationToken);
    }
}