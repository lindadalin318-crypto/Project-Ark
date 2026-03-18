namespace Astrolabe.Core;

/// <summary>
/// 单次 advisor 决策的统一日志结构。
/// Phase 1 先记录推荐侧数据，后续再补玩家最终选择与结果回写。
/// </summary>
public class DecisionRecord
{
    public int SchemaVersion { get; set; } = 1;
    public string TraceId { get; set; } = string.Empty;
    public DecisionKind Kind { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public RunSnapshot Snapshot { get; set; } = new();
    public List<DecisionPathStateSnapshot> ActivePaths { get; set; } = new();
    public List<DecisionCandidate> Candidates { get; set; } = new();
    public List<string> RecommendedOptionIds { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public string Why { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public string RiskLevel { get; set; } = "unknown";
    public List<string> AlternativeOptionIds { get; set; } = new();
    public string? PlayerChoiceId { get; set; }
    public bool? PlayerFollowedAdvice { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
