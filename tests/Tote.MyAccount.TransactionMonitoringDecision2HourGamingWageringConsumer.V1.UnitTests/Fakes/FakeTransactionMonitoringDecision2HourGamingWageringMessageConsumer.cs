using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tote.Packages.Kafka.Avro.Consumer;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1.UnitTests.Fakes;

public class FakeTransactionMonitoringDecision2HourGamingWageringMessageConsumer()
    : TransactionMonitoringDecision2HourGamingWageringMessageConsumer(new ConsumerConfig(), new KafkaConsumerTopicConfiguration(), Mock.Of<ISchemaRegistryClient>(), new NullLogger<KafkaAvroConsumerService<TransactionMonitoringDecision2HourGamingWagering>>())
{
    public Task Consume(ConsumeResult<string, TransactionMonitoringDecision2HourGamingWagering> result) => base.Consume(result);
}
