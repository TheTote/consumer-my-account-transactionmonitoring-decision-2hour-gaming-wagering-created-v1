using System.Globalization;
using System.Net;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1.UnitTests;

public class CoreMessagingApiClientTests
{
    private readonly string _baseAddress;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IOptionsMonitor<MessageSettings>> _messageSettingsMock;
    private readonly CoreMessagingApiClient _coreMessagingApiClient;

    public CoreMessagingApiClientTests()
    {
        _baseAddress = "https://example.com";
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_baseAddress)
        };
        _messageSettingsMock = new Mock<IOptionsMonitor<MessageSettings>>();
        _coreMessagingApiClient = new CoreMessagingApiClient(httpClient, _messageSettingsMock.Object);
    }
    
    [Fact]
    public async Task CreateMessage_SendsCorrectPostRequest()
    {
        // Arrange
        const string customerMasterId = "CUST-CUST-3BB6F756-5815-4CA3-9DFE-925B9EE6CFC4";
        var effectiveDateTimeUtc = DateTimeOffset.Parse("2025-01-01T00:00:00Z", CultureInfo.InvariantCulture);

        var messageSettings = new MessageSettings
        {
            Title = "Test Title",
            Texts = ["Lorem Ipsum", "Problem Gambler"],
            Type = "MarkersOfHarm",
            Buttons =
            [
                new MessageButtonConfig { Action = "CLOSE_POPUP", DisplayText = "Close", Tag = "close" }
            ]
        };

        _messageSettingsMock.Setup(callTo => callTo.CurrentValue)
                            .Returns(messageSettings);
        
        HttpRequestMessage? capturedRequest = null;

        SetupResponse(HttpStatusCode.OK, (req, _) =>
        {
            capturedRequest = req;
        });

        // Act
        await _coreMessagingApiClient.CreateMessage(customerMasterId, effectiveDateTimeUtc);

        // Assert
        const string expectedPath = $"/v1/customers/{customerMasterId}/messages";
        var expectedUri = _baseAddress + expectedPath;
        const string expectedBody = "{\"message\":{\"customerMasterId\":\"CUST-CUST-3BB6F756-5815-4CA3-9DFE-925B9EE6CFC4\",\"effectiveDateTimeUTC\":\"2025-01-01T00:00:00+00:00\",\"type\":\"MarkersOfHarm\",\"content\":{\"title\":\"Test Title\",\"texts\":[{\"order\":0,\"text\":\"Lorem Ipsum\"},{\"order\":1,\"text\":\"Problem Gambler\"}],\"buttons\":[{\"order\":0,\"displayText\":\"Close\",\"action\":\"CLOSE_POPUP\",\"actionExternalUrl\":null,\"tag\":\"close\"}]}}}";

        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal(expectedUri, capturedRequest.RequestUri!.ToString());
        var body = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Equal(expectedBody, body);
    }

    [Fact]
    public async Task EditSuspension_ThrowsIfNotSuccessful()
    {
        // Arrange
        var customerMasterId = $"CUST-CUST-{Guid.NewGuid()}";
        var effectiveDateTimeUtc = DateTimeOffset.UtcNow;

        _messageSettingsMock.Setup(callTo => callTo.CurrentValue)
                            .Returns(new MessageSettings
                            {
                                Title = "Test Title",
                                Texts = ["Loreum Ipsum", "Problem Gambler"],
                                Type = "Markers of Harm",
                                Buttons =
                                [
                                    new MessageButtonConfig { Action = "CLOSE_POPUP", DisplayText = "Close", Tag = "close" }
                                ]
                            });

        SetupResponse(HttpStatusCode.InternalServerError);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _coreMessagingApiClient.CreateMessage(customerMasterId, effectiveDateTimeUtc));
    }

    private void SetupResponse(HttpStatusCode status, Action<HttpRequestMessage, CancellationToken>? callback = null)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post
                ),
                ItExpr.IsAny<CancellationToken>())
            .Callback(callback ?? ((_, _) => { }))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status
            })
            .Verifiable();
    }
}
