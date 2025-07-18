using Moq;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1.UnitTests;

public class SlackMessageSenderTests
{
    private readonly string _topicName;
    private readonly Mock<IKafkaJsonProducer<string>> _producerMock;
    private readonly SlackMessageSender _sender;

    public SlackMessageSenderTests()
    {
        _topicName = "slack-kafka-topic";
        _producerMock = new Mock<IKafkaJsonProducer<string>>();
        _sender = new SlackMessageSender(_topicName, _producerMock.Object);
    }

    [Fact]
    public async Task Publish_CorrectFormat()
    {
        // Arrange
        var message = new SlackNotificationMessage { Channel = "test-slack-channel", Message = "Test Message" };
        
        // Act
        await _sender.Publish(message);
        
        // Assert
        _producerMock.Verify(callTo => callTo.ProduceAsync(_topicName, message.Channel, message, null, CancellationToken.None), Times.Once);
    }
}
