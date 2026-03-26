namespace ProjectArk.Level
{
    /// <summary>
    /// 门/通道连接的语义类型。
    /// 来源：Minishoot 分析 1.23 的 5 类连接关系 + Ark 的时间连接。
    /// 
    /// 每种连接回答一个问题：
    /// - Progression: "我还能往前走吗？"
    /// - Return: "我如果回头，会不会更快？"
    /// - Ability: "这里先记住，以后拿到能力再回来。"
    /// - Challenge: "你现在有没有资格进入更高压内容？"
    /// - Identity: "你刚刚进入了什么类型的地方？"
    /// - Scheduled: "现在是不是正确的时间？"（Ark 特有）
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>推进连接：主路线推进，通常连接压力段或新区域。</summary>
        Progression,

        /// <summary>回返连接：捷径，重新连接旧安全点。</summary>
        Return,

        /// <summary>能力连接：银河城能力门，暂时性视觉钩子。</summary>
        Ability,

        /// <summary>挑战连接：Arena/Boss 前厅/支线挑战房。</summary>
        Challenge,

        /// <summary>身份连接：Biome 切换、空间章节分割。</summary>
        Identity,

        /// <summary>时间连接：由 WorldPhase 控制开关（Ark 特有）。</summary>
        Scheduled
    }
}
