using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tote.MyAccount.TransactionMonitoringDecision2HourGamingWageringConsumer.V1.StartupConfiguration;

[ExcludeFromCodeCoverage]
public static class HealthCheckWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport result)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        JsonWriterOptions options = new() { Indented = true };

        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream, options))
        {
            writer.WriteStartObject();
            writer.WriteString("status", result.Status.ToString());
            writer.WriteNumber("executionMilliseconds", result.TotalDuration.TotalMilliseconds);
            writer.WriteStartObject("results");
            foreach ((string s, HealthReportEntry healthReportEntry) in result.Entries)
            {
                writer.WriteStartObject(s);
                writer.WriteString("status", healthReportEntry.Status.ToString());
                writer.WriteString("description", healthReportEntry.Description ?? string.Empty);
                writer.WriteString("exception", healthReportEntry.Exception?.Message ?? string.Empty);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        var json = Encoding.UTF8.GetString(stream.ToArray());

        return context.Response.WriteAsync(json);
    }
}