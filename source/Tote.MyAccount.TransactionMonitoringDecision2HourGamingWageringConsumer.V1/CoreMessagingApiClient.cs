using Microsoft.Extensions.Options;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1;

public interface ICoreMessagingApiClient
{
    Task CreateMessage(string customerMasterId, DateTimeOffset effectiveDateTimeUtc, Action<List<string>>? textsDelegate = null);
}

public class CoreMessagingApiClient(HttpClient client, IOptionsMonitor<MessageSettings> messageSettings) : ICoreMessagingApiClient
{
    public async Task CreateMessage(string customerMasterId, DateTimeOffset effectiveDateTimeUtc, Action<List<string>>? textsDelegate = null)
    {
        var settings = messageSettings.CurrentValue;

        textsDelegate?.Invoke(settings.Texts);
        var result = await client.PostAsJsonAsync($"v1/customers/{customerMasterId}/messages", new
        {
            Message = new CreateMessageBody(
                customerMasterId,
                effectiveDateTimeUtc,
                settings.Type,
                new CreateMessageContent(
                    settings.Title,
                    settings.Texts.Select((t, i) => new CreateMessageText(i, t)).ToArray().AsReadOnly(),
                    settings.Buttons.Select((b, i) => new CreateMessageButton(i, b.DisplayText, b.Action, b.ActionExternalUrl, b.Tag)).ToArray().AsReadOnly()
                )
            )
        }, CancellationToken.None);
        result.EnsureSuccessStatusCode();
    }

    private sealed record CreateMessageBody(string CustomerMasterId, DateTimeOffset EffectiveDateTimeUTC, string Type, CreateMessageContent Content);
    private sealed record CreateMessageContent(string Title, IReadOnlyCollection<CreateMessageText> Texts, IReadOnlyCollection<CreateMessageButton> Buttons);
    private sealed record CreateMessageText(int Order, string Text);
    private sealed record CreateMessageButton(int Order, string DisplayText, string Action, string? ActionExternalUrl, string Tag);
}