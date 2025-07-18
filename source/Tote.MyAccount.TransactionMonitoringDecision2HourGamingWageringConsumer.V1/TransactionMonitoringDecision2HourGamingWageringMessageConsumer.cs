using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.Logging;
using Tote.Packages.Kafka.Avro.Consumer;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1;

public class TransactionMonitoringDecision2HourGamingWageringMessageConsumer(
    ConsumerConfig config,
    KafkaConsumerConfiguration topicConfiguration,
    ISchemaRegistryClient schemaRegistryClient,
    ILogger<KafkaAvroConsumerService<TransactionMonitoringDecision2HourGamingWagering>> logger)
    : KafkaAvroConsumerService<TransactionMonitoringDecision2HourGamingWagering>(config, topicConfiguration, schemaRegistryClient, logger)
{
    protected override Task Consume(ConsumeResult<string, TransactionMonitoringDecision2HourGamingWagering> result)
    {
        return Task.CompletedTask;
    }
}
