namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1;

public sealed record MessageSettings
{
    public required string Type { get; init; }

    public required string Title { get; init; }
    
    public required List<string> Texts { get; init; }
    
    public required List<MessageButtonConfig> Buttons { get; init; }
}

public sealed record MessageButtonConfig
{
    public required string DisplayText { get; set; }
    public required string Action { get; set; }
    public string? ActionExternalUrl { get; set; }
    public required string Tag { get; set; }
}