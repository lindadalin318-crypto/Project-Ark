namespace Astrolabe.Core;

/// <summary>
/// 单个候选项的统一记录结构。
/// </summary>
public class DecisionCandidate
{
    public string OptionId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public float? Score { get; set; }
    public string Rating { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
