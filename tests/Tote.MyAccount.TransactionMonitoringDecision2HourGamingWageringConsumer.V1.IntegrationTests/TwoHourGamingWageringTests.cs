using Microsoft.VisualStudio.TestPlatform.TestHost;
using Tote.TransactionMonitoringApi.Events;
using Xunit;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1.IntegrationTests;

public class TwoHourGamingWageringTests : IAsyncLifetime, IClassFixture<KafkaFixture>, IClassFixture<WireMockFixture>
{
    private const int HealthCheckPort = 5049;
    private readonly KafkaFixture _kafka;
    private readonly WireMockFixture _wireMock;

    public TwoHourGamingWageringTests(KafkaFixture kafka, WireMockFixture wireMock)
    {
        Environment.SetEnvironmentVariable("Service:Ports:Health", HealthCheckPort.ToString());
        Environment.SetEnvironmentVariable("ervice:BackOffice:BaseAddress", "https://api.tote.rocks");
        Environment.SetEnvironmentVariable("Service:Slack:ChannelName", "stop-sell-test");
        _kafka = kafka;
        _wireMock = wireMock;
    }
    
    [Fact]
    public async Task Trigger2HourGamingWagering_CreateMessage()
    {
        // Arrange
        var decision = new TransactionMonitoringDecision
        {
            CustomerMasterId = $"CUST-CUST-{Guid.NewGuid()}",
            LegacyUserId = "500030",
            Value = "Next",
            InitiatedByMasterId = $"BETS-BETS-{Guid.NewGuid()}",
            InitiatedDateTimeUtc = DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds(),
            RuleTriggerDateTimeUtc = DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds(),
            RuleCode = "2HourGamingWagering",
            TransactionMonitoringDecisionMasterId = $"TRAN-DECI-{Guid.NewGuid()}"
        };

        // Act
        var result = await _kafka.PublishEvent(decision.CustomerMasterId, decision);
        
        // Assert
        await _kafka.EnsureConsumed(result);
        Assert.True(_wireMock.VerifyCallToCreateMessage());
    }

    [Fact]
    public async Task Trigger2HourGamingWagering_SendToSlack()
    {
        // Arrange
        var decision = new TransactionMonitoringDecision
        {
            CustomerMasterId = $"CUST-CUST-{Guid.NewGuid()}",
            LegacyUserId = "500030",
            Value = "Next",
            InitiatedByMasterId = $"BETS-BETS-{Guid.NewGuid()}",
            InitiatedDateTimeUtc = DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds(),
            RuleTriggerDateTimeUtc = DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds(),
            RuleCode = "2HourGamingWagering",
            TransactionMonitoringDecisionMasterId = $"TRAN-DECI-{Guid.NewGuid()}"
        };

        var startSendSlackMessageCount = _kafka.CaptureSendSlackMessageCount();

        // Act
        var result = await _kafka.PublishEvent(decision.CustomerMasterId, decision);
        
        // Assert
        await _kafka.EnsureConsumed(result);
        var endSendSlackMessageCount = _kafka.CaptureSendSlackMessageCount();
        Assert.Equal(1, endSendSlackMessageCount - startSendSlackMessageCount);
    }

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "IntegrationTests");
        Task.Run(() => Program.Main(null));
        
        using var client = new HttpClient();
        var ready = false;
        for (var i = 0; i < 10 && !ready; i++)
        {
            try
            {
                var healthCheckResponse = await client.GetAsync($"http://localhost:{HealthCheckPort}/healthz/ready");
                ready = healthCheckResponse.IsSuccessStatusCode;
            }
            catch
            {
                await Task.Delay(500);
            }
        }

        if (!ready)
        {
            throw new Exception("App did not become ready.");
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}