using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1.IntegrationTests;

public sealed class WireMockFixture : IDisposable
{
    private readonly WireMockServer _server;

    public WireMockFixture()
    {
        _server = WireMockServer.Start(new WireMockServerSettings
        {
            StartAdminInterface = false,
            ReadStaticMappings = false
        });
        MockCoreMessagingApi();
    }
    
    
    private void MockCoreMessagingApi()
    {
        _server.Given(
                Request.Create()
                    .WithPath("/v1/customers/*/messages")
                    .UsingPost()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(201)
            );
        Environment.SetEnvironmentVariable("Service:CoreMessagingApi:BaseAddress", _server.Url);
    }

    public bool VerifyCallToCreateMessage()
    {
        return _server.LogEntries.Any(entry =>
            entry.RequestMessage.Method == "POST" &&
            entry.RequestMessage.Path.StartsWith("/v1/customers/") &&
            entry.RequestMessage.Path.EndsWith("/messages")
        );
    }

    public void Dispose()
    {
        _server.Dispose();
    }
}