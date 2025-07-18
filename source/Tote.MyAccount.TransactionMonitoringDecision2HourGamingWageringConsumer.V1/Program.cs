using Confluent.Kafka;
using Prometheus;
using Serilog;
using Tote.Packages.HealthChecks;
using Tote.Packages.Kafka.Avro.Consumer;
using Tote.TransactionMonitoringApi.Events;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1;

public static class Program
{
    public static async Task<int> Main(params string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = Host.CreateApplicationBuilder(args);
            
            var healthCheckHostBuilder = WebApplication.CreateSlimBuilder(args);
            healthCheckHostBuilder.WebHost.ConfigureKestrel(options =>
            {
                var port = healthCheckHostBuilder.Configuration.GetValue<ushort>("service:ports:health");
                options.ListenAnyIP(port);
            });
            healthCheckHostBuilder.Services.AddHealthChecks();
            var healthCheckHost = healthCheckHostBuilder.Build();
            healthCheckHost.MapHealthCheckEndpoints();

            builder.Services.AddMetricServer(options =>
                options.Port = builder.Configuration.GetValue<ushort>("service:ports:metrics"));

            builder.Services.AddSerilog((_, lc) => lc
                .ReadFrom.Configuration(builder.Configuration));
            
            builder.Services.Configure<MessageSettings>(
                builder.Configuration.GetSection("Service:MessageSettings"));

            builder.Services.AddHttpClient<ICoreMessagingApiClient, CoreMessagingApiClient>(client =>
            {
                var baseAddress = builder.Configuration.GetValue<string>("Service:CoreMessagingApi:BaseAddress") ??
                                  throw new ArgumentException("Core bonus api base address be provided.");
                client.BaseAddress = new Uri(baseAddress);
            });

            builder.Services.AddTransient<IKafkaJsonProducer<string>>(_ =>
            {
                var bootstrapServers = string.Join(",", builder.Configuration.GetSection("Service:Kafka:BootstrapServers").Get<string[]>() ?? []);
                var producerConfig = new ProducerConfig
                {
                    BootstrapServers = bootstrapServers
                };
                return new KafkaJsonProducer<string>(producerConfig);
            });

            builder.Services.AddTransient<ISlackMessageSender>(sp =>
            {
                var producer = sp.GetRequiredService<IKafkaJsonProducer<string>>();
                var topicName = builder.Configuration.GetValue<string>("Service:Kafka:Producer:Topics:Slack");

                return new SlackMessageSender(topicName, producer);
            });

            builder.AddKafkaAvroConsumer<TransactionMonitoringDecision2HourGamingWageringMessageConsumer, TransactionMonitoringDecision>();

            var host = builder.Build();
            await healthCheckHost.StartAsync();
            await host.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An unhandled exception occurred during bootstrapping");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
