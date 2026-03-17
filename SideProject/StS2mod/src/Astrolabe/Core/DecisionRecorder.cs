using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Logging;

namespace Astrolabe.Core;

/// <summary>
/// 将统一的 <see cref="DecisionRecord"/> 追加写入 JSONL 日志。
/// 先服务离线评测与 replay 样本采集，后续再扩展查询与回写链路。
/// </summary>
public static class DecisionRecorder
{
    private static readonly object _gate = new();
    private static readonly Logger _log = new("Astrolabe.DecisionRecorder", LogType.Generic);
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    private static string? _recordDirectory;

    public static void Initialize()
    {
        EnsureInitialized();
    }

    public static void Record(DecisionRecord record)
    {
        try
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(record.TraceId))
                record.TraceId = DecisionTraceId.Create(record.Kind);

            if (record.RecordedAtUtc == default)
                record.RecordedAtUtc = DateTime.UtcNow;

            string filePath = Path.Combine(
                _recordDirectory!,
                $"decision-records-{record.RecordedAtUtc:yyyy-MM-dd}.jsonl");

            string payload = JsonSerializer.Serialize(record, _jsonOptions) + Environment.NewLine;

            lock (_gate)
            {
                File.AppendAllText(filePath, payload);
            }
        }
        catch (Exception ex)
        {
            _log.Warn($"[DecisionRecorder] Failed to append decision record: {ex.Message}");
        }
    }

    private static void EnsureInitialized()
    {
        if (!string.IsNullOrWhiteSpace(_recordDirectory))
            return;

        lock (_gate)
        {
            if (!string.IsNullOrWhiteSpace(_recordDirectory))
                return;

            _recordDirectory = ResolveRecordDirectory();
            Directory.CreateDirectory(_recordDirectory);
            _log.Info($"[DecisionRecorder] Record directory ready: {_recordDirectory}");
        }
    }

    private static string ResolveRecordDirectory()
    {
        string execDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? Directory.GetCurrentDirectory();

        return Path.Combine(execDir, "telemetry", "decision-records");
    }

    private static string GenerateTraceId(DecisionKind kind)
    {
        string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"{kind.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{suffix}";
    }
}
