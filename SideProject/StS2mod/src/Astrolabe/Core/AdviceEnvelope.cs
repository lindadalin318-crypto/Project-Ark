using System.Collections.Generic;

namespace Astrolabe.Core;

/// <summary>
/// UI 展示与 telemetry 记录共用的统一建议包装。
/// Phase 1 先打通 traceId / summary / why / confidence / risk 的单向闭环。
/// </summary>
public sealed class AdviceEnvelope<T>
{
    public string TraceId { get; init; } = string.Empty;
    public string? ParentTraceId { get; init; }
    public DecisionKind Kind { get; init; }
    public string Source { get; init; } = string.Empty;
    public T Payload { get; init; } = default!;
    public string Summary { get; init; } = string.Empty;
    public string Why { get; init; } = string.Empty;
    public float Confidence { get; init; }
    public string RiskLevel { get; init; } = "unknown";
    public List<string> RecommendedOptionIds { get; init; } = new();
    public List<string> AlternativeOptionIds { get; init; } = new();
    public Dictionary<string, string> Metadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public static AdviceEnvelope<T> FromRecord(
        DecisionRecord record,
        T payload,
        string? parentTraceId = null)
    {
        return new AdviceEnvelope<T>
        {
            TraceId = record.TraceId,
            ParentTraceId = parentTraceId,
            Kind = record.Kind,
            Source = record.Source,
            Payload = payload,
            Summary = record.Summary,
            Why = record.Why,
            Confidence = record.Confidence,
            RiskLevel = record.RiskLevel,
            RecommendedOptionIds = new List<string>(record.RecommendedOptionIds),
            AlternativeOptionIds = new List<string>(record.AlternativeOptionIds),
            Metadata = new Dictionary<string, string>(record.Metadata, StringComparer.OrdinalIgnoreCase),
        };
    }
}
