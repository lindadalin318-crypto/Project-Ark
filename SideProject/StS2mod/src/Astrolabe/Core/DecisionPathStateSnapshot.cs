namespace Astrolabe.Core;

/// <summary>
/// 决策发生时的 BuildPath 状态快照。
/// </summary>
public class DecisionPathStateSnapshot
{
    public string PathId { get; set; } = string.Empty;
    public string NameZh { get; set; } = string.Empty;
    public float Viability { get; set; }
    public string Trend { get; set; } = string.Empty;
}
