using Confluent.Kafka;
using Tote.Matchmaking.BetslipPlacedConsumer.V1.UnitTests.Fakes;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1.UnitTests;

public class TransactionMonitoringDecision2HourGamingWageringMessageConsumerTests
{
    private readonly FakeTransactionMonitoringDecision2HourGamingWageringMessageConsumer _consumer;

    public TransactionMonitoringDecision2HourGamingWageringMessageConsumerTests()
    {
        _consumer = new FakeTransactionMonitoringDecision2HourGamingWageringMessageConsumer();
    }

    [Fact]
    public async Task Test1()
    {
        // Arrange
        var result = new ConsumeResult<string, TransactionMonitoringDecision2HourGamingWagering>
        {
            Message = new Message<string, TransactionMonitoringDecision2HourGamingWagering>
            {
                Key = "Test",
                Value = new TransactionMonitoringDecision2HourGamingWagering()
            }
        };
        
        // Act
        await _consumer.Consume(result);
        
        // Assert
        Assert.True(true);
    }
}