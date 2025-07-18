namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1;

public interface ISlackMessageSender
{
    Task Publish(SlackNotificationMessage message);
}

public class SlackMessageSender(string topicName, IKafkaJsonProducer<string> producer) : ISlackMessageSender
{
    public async Task Publish(SlackNotificationMessage message)
    {
        await producer.ProduceAsync(
            topicName,
            message.Channel,
            message);
    }
}

public sealed record SlackNotificationMessage
{
    public required string Channel { get; init; }
    public required string Message { get; init; }
}