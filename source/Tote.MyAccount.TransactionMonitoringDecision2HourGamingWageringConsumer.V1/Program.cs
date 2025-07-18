using Microsoft.Extensions.Hosting;
using Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1;
using Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1.StartupConfiguration;
using Tote.Packages.Kafka.Avro.Consumer;
using System.Net;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Prometheus;
using Serilog;


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddHealthChecks();

    var healthCheckHost = new WebHostBuilder()
        .UseKestrel()
        .UseIISIntegration()
        .ConfigureServices(services =>
        {
            services.AddHealthChecks();
            services.Configure<KestrelServerOptions>(options =>
            {
                options.Listen(IPAddress.Any, builder.Configuration.GetValue<ushort>("service:ports:health"));
            });
            services.AddRouting();
        })
        .Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks(
                    "/healthz/live",
                    new HealthCheckOptions
                    {
                        Predicate = r => r.Name.Contains("self"),
                        ResponseWriter = HealthCheckWriter.WriteResponse,
                    });
                endpoints.MapHealthChecks(
                    "/healthz/ready",
                    new HealthCheckOptions
                    {
                        Predicate = r => r.Name.Contains("self"),
                        ResponseWriter = HealthCheckWriter.WriteResponse,
                    });
            });
        })
        .Build();

    builder.Services.AddMetricServer(options =>
        options.Port = builder.Configuration.GetValue<ushort>("service:ports:metrics"));

    builder.Services.AddSerilog((_, lc) => lc
        .ReadFrom.Configuration(builder.Configuration));

    builder.AddKafkaAvroConsumer<TransactionMonitoringDecision2HourGamingWageringMessageConsumer, TransactionMonitoringDecision2HourGamingWagering>();

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