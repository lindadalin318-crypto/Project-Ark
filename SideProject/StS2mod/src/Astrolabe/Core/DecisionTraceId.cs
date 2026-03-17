namespace Astrolabe.Core;

/// <summary>
/// 决策 traceId 生成器。
/// 由 AdvisorEngine 在记录前生成，DecisionRecorder 保留空值兜底。
/// </summary>
public static class DecisionTraceId
{
    public static string Create(DecisionKind kind)
    {
        string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"{kind.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{suffix}";
    }
}
