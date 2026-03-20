using System.IO;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Project.HttpServer.Monitoring.Tracing;

public static class TelemetryRegistration
{
    public static IServiceCollection AddTracingTelemetry(this IServiceCollection services, TelemetryOptions options)
    {
        var exportFilePath = Path.Combine(options.ExportPath, "traces.jsonl");
        var exporter = new JsonlActivityExporter(exportFilePath);
        services.AddSingleton(exporter);

        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Project.HttpServer"))
                    .SetSampler(new AlwaysOnSampler())
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                // Подключаем кастомный exporter поверх Activity pipeline.
                builder.AddProcessor(new SimpleActivityExportProcessor(exporter));
            });

        return services;
    }
}

