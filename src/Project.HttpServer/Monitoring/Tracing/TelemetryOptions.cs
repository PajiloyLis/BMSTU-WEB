namespace Project.HttpServer.Monitoring.Tracing;

public sealed class TelemetryOptions
{
    public bool TracingEnabled { get; init; }
    public string ExportPath { get; init; } = "./telemetry";
}

