using System.Diagnostics;
using Confluent.Kafka;
using Tote.Packages.Kafka.Avro.Consumer;
using Tote.TransactionMonitoringApi.Events;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1;

public class TransactionMonitoringDecision2HourGamingWageringMessageConsumer(
    ICoreMessagingApiClient messagingApiClient,
    ISlackMessageSender slackMessageSender,
    InstrumentedConsumerBuilder<string, TransactionMonitoringDecision> consumerBuilder,
    IConfiguration configuration,
    ILogger<TransactionMonitoringDecision2HourGamingWageringMessageConsumer> logger)
    : KafkaAvroConsumerService<TransactionMonitoringDecision>(consumerBuilder, configuration, logger)
{
    private readonly string _slackChannelName = configuration.GetValue<string>("Service:Slack:ChannelName")
                                                ?? throw new ArgumentException("Slack channel name not configured");

    protected override async ValueTask ProcessMessage(ConsumeResult<string, TransactionMonitoringDecision> consumeResult, Activity? activity, CancellationToken cancellationToken)
    {
        var decision = consumeResult.Message.Value;
        
        var ruleTriggerDateTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(decision.RuleTriggerDateTimeUtc);
        
        var backOfficeBaseUrl = configuration.GetValue<string>("Service:BackOffice:BaseAddress");
        var customerViewUrl = new Uri($"{backOfficeBaseUrl}/crm/users/{decision.LegacyUserId}/view");

        var slackMessageMarkup = new SlackMessageBuilder().AppendKey("Customer ID")
                                                          .AppendLink(decision.LegacyUserId, customerViewUrl)
                                                          .AppendKey("Reason")
                                                          .AppendStringValue("2+ hours gaming during at-risk hours")
                                                          .Build();
        
        var slackMessage = new SlackNotificationMessage
        {
            Channel = _slackChannelName,
            Message = slackMessageMarkup
        };

        await Task.WhenAll(
            messagingApiClient.CreateMessage(decision.CustomerMasterId, ruleTriggerDateTimeUtc),
            slackMessageSender.Publish(slackMessage)
        );
    }
}
