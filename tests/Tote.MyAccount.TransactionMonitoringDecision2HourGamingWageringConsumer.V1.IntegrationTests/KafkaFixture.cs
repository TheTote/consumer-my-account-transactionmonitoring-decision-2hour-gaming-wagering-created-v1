using System.Diagnostics;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Tote.TransactionMonitoringApi.Events;
using Xunit;
using ContainerBuilder = DotNet.Testcontainers.Builders.ContainerBuilder;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1.IntegrationTests;

public sealed class KafkaFixture : IAsyncLifetime
{
    private IAdminClient _adminClient;
    private IProducer<string, TransactionMonitoringDecision> _producer;
    private IConsumer<Ignore, Ignore> _consumer;
    private TopicPartition _loneSlackTopicPartition;

    private const string _consumerTopicName = "trxmonitoring.2HourGamingWagering";
    private const string _consumerGroupId = "consumer-trxmonitoring2HourGamingWagering";
    private const string _slackTopicName = "slack.create.message";
    private const string KafkaUIName = "consumer_trxmonitoring2HourGamingWagering_kafkaui";
    private const string KafkaServiceName = "consumer_trxmonitoring2HourGamingWagering_kafka";
    private const string SchemaRegistryName = "consumer_trxmonitoring2HourGamingWagering_schema_registry";

    private const int KafkaUIPort = 49092;
    private const int KafkaDockerPort = 39092;
    private const int KafkaInternalPort = 29092;
    private const int KafkaExternalPort = 19092;
    private const int ControllerPort = 9093;
    private const int SchemaRegistryPort = 8081;
    private static INetwork Network { get; } = new NetworkBuilder().WithName("consumer_trxmonitoring2HourGamingWagering_kafka_network").Build();
    private static IContainer KafkaContainer { get; } = new ContainerBuilder()
        .WithName(KafkaServiceName)
        .WithImage("confluentinc/cp-kafka:latest")
        .WithHostname(KafkaServiceName)
        .WithPortBinding(KafkaDockerPort)
        .WithPortBinding(KafkaExternalPort)
        .WithNetwork(Network)
        .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
        .WithEnvironment("KAFKA_NODE_ID", "1")
        .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", $"1@{KafkaServiceName}:{ControllerPort}")
        .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
        .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "INTERNAL")
        .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "INTERNAL:PLAINTEXT,EXTERNAL:PLAINTEXT,DOCKER:PLAINTEXT,CONTROLLER:PLAINTEXT")
        .WithEnvironment("KAFKA_LISTENERS", $"INTERNAL://{KafkaServiceName}:{KafkaInternalPort},EXTERNAL://0.0.0.0:{KafkaExternalPort},DOCKER://0.0.0.0:{KafkaDockerPort},CONTROLLER://{KafkaServiceName}:{ControllerPort}")
        .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", $"INTERNAL://{KafkaServiceName}:{KafkaInternalPort},EXTERNAL://localhost:{KafkaExternalPort},DOCKER://host.docker.internal:{KafkaDockerPort}")
        .WithEnvironment("KAFKA_LOG_DIRS", "/var/lib/kafka/data")
        .WithEnvironment("KAFKA_METADATA_LOG_DIR", "/var/lib/kafka/meta")
        .WithEntrypoint("/bin/sh", "-c")
        .WithCommand(
            "sed -i \"s|^listener.security.protocol.map=.*|listener.security.protocol.map=${KAFKA_LISTENER_SECURITY_PROTOCOL_MAP}|\" /etc/kafka/kraft/server.properties && " +
            "sed -i \"s|^controller.quorum.voters=.*|controller.quorum.voters=${KAFKA_CONTROLLER_QUORUM_VOTERS}|\" /etc/kafka/kraft/server.properties && " +
            "sed -i \"s|^controller.listener.names=.*|controller.listener.names=${KAFKA_CONTROLLER_LISTENER_NAMES}|\" /etc/kafka/kraft/server.properties && " +
            "sed -i \"s|^inter.broker.listener.name=.*|inter.broker.listener.name=${KAFKA_INTER_BROKER_LISTENER_NAME}|\" /etc/kafka/kraft/server.properties && " +
            "sed -i \"s|^listeners=.*|listeners=${KAFKA_LISTENERS}|\" /etc/kafka/kraft/server.properties && " +
            "sed -i \"s|^advertised.listeners=.*|advertised.listeners=${KAFKA_ADVERTISED_LISTENERS}|\" /etc/kafka/kraft/server.properties && " +
            "CLUSTER_ID=$(/usr/bin/kafka-storage random-uuid) && " +
            "/usr/bin/kafka-storage format -t $CLUSTER_ID -c /etc/kafka/kraft/server.properties && " +
            "/usr/bin/kafka-server-start /etc/kafka/kraft/server.properties"
        )
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Kafka Server started"))
        .Build();
    
    private static IContainer SchemaRegistryContainer { get; } = new ContainerBuilder()
        .DependsOn(KafkaContainer)
        .WithName(SchemaRegistryName)
        .WithImage("confluentinc/cp-schema-registry:latest")
        .WithPortBinding(SchemaRegistryPort)
        .WithNetwork(Network)
        .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", $"{KafkaServiceName}:{KafkaInternalPort}")
        .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", SchemaRegistryName)
        .WithEnvironment("SCHEMA_REGISTRY_LISTENERS", $"http://0.0.0.0:{SchemaRegistryPort}")
        .WithEnvironment("SCHEMA_REGISTRY_DEBUG", "true")
        .WithEnvironment("SCHEMA_REGISTRY_AVRO_COMPATIBILITY_LEVEL", "backward")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(SchemaRegistryPort))
        .Build();

    private static IContainer KafkaUIContainer { get; } = new ContainerBuilder()
        .DependsOn(KafkaContainer)
        .DependsOn(SchemaRegistryContainer)
        .WithName(KafkaUIName)
        .WithImage("ghcr.io/kafbat/kafka-ui:latest")
        .WithPortBinding(KafkaUIPort, 8080)
        .WithNetwork(Network)
        .WithEnvironment("KAFKA_CLUSTERS_0_NAME", KafkaServiceName)
        .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", $"host.docker.internal:{KafkaDockerPort}")
        .WithEnvironment("KAFKA_CLUSTERS_0_SCHEMAREGISTRY", $"http://host.docker.internal:{SchemaRegistryPort}")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(8080).ForPath("/")))
        .Build();

    public async Task InitializeAsync()
    {
        await KafkaContainer.StartAsync();
    
        await SchemaRegistryContainer.StartAsync();

        if (Debugger.IsAttached) await KafkaUIContainer.StartAsync();
        
        var adminClientConfig = new AdminClientConfig
        {
            BootstrapServers = $"PLAINTEXT://localhost:{KafkaExternalPort}"
        };
        _adminClient = new AdminClientBuilder(adminClientConfig).Build();
        
        var producerConfig = new AdminClientConfig
        {
            BootstrapServers = $"PLAINTEXT://localhost:{KafkaExternalPort}"
        };
        _producer = new ProducerBuilder<string, TransactionMonitoringDecision>(producerConfig)
            .SetValueSerializer(new AvroSerializer<TransactionMonitoringDecision>(
                new CachedSchemaRegistryClient(
                    new SchemaRegistryConfig { Url = $"http://localhost:{SchemaRegistryPort}" }),
                new AvroSerializerConfig
                {
                    AutoRegisterSchemas = true, SubjectNameStrategy = SubjectNameStrategy.TopicRecord
                }).AsSyncOverAsync())
            .Build();
        
        
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = $"PLAINTEXT://localhost:{KafkaExternalPort}",
            GroupId = _consumerGroupId,
            EnableAutoCommit = false
        };
        _consumer = new ConsumerBuilder<Ignore, Ignore>(consumerConfig).Build();

        await _adminClient.CreateTopicsAsync([
            new TopicSpecification
            {
                Name = _slackTopicName,
                NumPartitions = 1,
                ReplicationFactor = 1
            },
            new TopicSpecification
            {
                Name = _consumerTopicName,
                NumPartitions = 1,
                ReplicationFactor = 1
            }
        ]);
        
        var slackTopicMetadata = _adminClient.GetMetadata(_slackTopicName, TimeSpan.FromMilliseconds(200)).Topics[0];
        var slackTopicPartitionId = slackTopicMetadata.Partitions[0].PartitionId;
        _loneSlackTopicPartition = new TopicPartition(_slackTopicName, slackTopicPartitionId);

        Environment.SetEnvironmentVariable("Service__Kafka__BootstrapServers__0", $"PLAINTEXT://localhost:{KafkaExternalPort}");
        Environment.SetEnvironmentVariable("Service__Kafka__SchemaRegistryUrls__0", $"http://localhost:{SchemaRegistryPort}");
        Environment.SetEnvironmentVariable("Service__Kafka__Producer__Topics__Slack", _slackTopicName);
        Environment.SetEnvironmentVariable("Service__Kafka__Consumer__GroupId", _consumerGroupId);
        Environment.SetEnvironmentVariable("Service__Kafka__Consumer__Topic", _consumerTopicName);
    }

    public async Task DisposeAsync()
    {
        _adminClient.Dispose();
        if (Debugger.IsAttached) await KafkaUIContainer.StopAsync();
        await SchemaRegistryContainer.StopAsync();
        await KafkaContainer.StopAsync();
    }

    public async Task<DeliveryResult<string, TransactionMonitoringDecision>> PublishEvent(string key, TransactionMonitoringDecision decision)
    {
        var result = await _producer.ProduceAsync(_consumerTopicName, new Message<string, TransactionMonitoringDecision>
        {
            Key = key,
            Value = decision
        });
        return result;
    }

    public async Task EnsureConsumed(DeliveryResult<string, TransactionMonitoringDecision> result)
    {
        var hasBeenConsumed = false;

        while (!hasBeenConsumed)
        {
            var offset = _consumer.Committed([result.TopicPartition], TimeSpan.FromMilliseconds(200))[0];
            if (offset.Offset.Value > result.Offset.Value)
            {
                hasBeenConsumed = true;
            }
            else
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }
    }

    public long CaptureSendSlackMessageCount()
    {
        var offsets = _consumer.QueryWatermarkOffsets(_loneSlackTopicPartition, TimeSpan.FromMilliseconds(200));
        return offsets.High - offsets.Low;
    }
}
