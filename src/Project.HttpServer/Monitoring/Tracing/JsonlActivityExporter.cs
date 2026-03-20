using System.Diagnostics;
using System.Text.Json;
using OpenTelemetry.Trace;

namespace Project.HttpServer.Monitoring.Tracing;

/// <summary>
/// Простейший файловый exporter: пишет Activity в JSON Lines.
/// Нужен, чтобы данные трассировки были доступны в CI/benchmark и сохранялись на диск.
/// </summary>
public sealed class JsonlActivityExporter : BaseExporter<Activity>
{
    private readonly string _exportFilePath;
    private readonly object _sync = new();
    private StreamWriter? _writer;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    public JsonlActivityExporter(string exportFilePath)
    {
        _exportFilePath = exportFilePath;
    }

    public override ExportResult Export(in Batch<Activity> batch)
    {
        EnsureWriter();

        if (_writer is null)
            return ExportResult.Failure;

        foreach (var activity in batch)
        {
            var record = new
            {
                activity.DisplayName,
                activity.Kind,
                activity.TraceId,
                activity.SpanId,
                activity.ParentSpanId,
                StartTimeUtc = activity.StartTimeUtc,
                DurationMs = activity.Duration.TotalMilliseconds,
                activity.Status,
                Tags = activity.Tags.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString())
            };

            var json = JsonSerializer.Serialize(record, _jsonOptions);
            _writer.WriteLine(json);
        }

        _writer.Flush();
        return ExportResult.Success;
    }

    private void EnsureWriter()
    {
        if (_writer is not null)
            return;

        lock (_sync)
        {
            if (_writer is not null)
                return;

            var? dir = Path.GetDirectoryName(_exportFilePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _writer = new StreamWriter(_exportFilePath, append: true);
        }
    }

    public override void Shutdown()
    {
        lock (_sync)
        {
            try
            {
                _writer?.Flush();
                _writer?.Dispose();
            }
            catch
            {
                // ignore shutdown issues
            }
            finally
            {
                _writer = null;
            }
        }
    }
}

