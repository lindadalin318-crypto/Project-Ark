namespace ProjectArk.Level
{
    /// <summary>
    /// 房间在关卡节奏中的职责类型。
    /// 来源：Minishoot 分析 1.21 的 6 种节点分级 + Ark 扩展。
    /// 
    /// 设计原则：不要说"我要做 8 个房间"，而要说"我要 2 个 Transit + 2 个 Pressure + 
    /// 1 个 Resolution + 1 个 Reward + 1 个 Anchor + 1 个 Loop"。
    /// 
    /// 替代旧 <see cref="RoomType"/>，两者并存过渡后将废弃 RoomType。
    /// </summary>
    public enum RoomNodeType
    {
        /// <summary>过路节点：保持移动，连接相邻区域，不承载太重信息。可能有少量 EncounterOpen。</summary>
        Transit,

        /// <summary>压力节点：不打断流程但增加消耗和警惕。1-3 组开放敌人，可边打边走。</summary>
        Pressure,

        /// <summary>清算节点：用封闭战斗验证玩家是否掌握当前区段规则。EncounterClose + 波次/Boss。</summary>
        Resolution,

        /// <summary>回报节点：紧张后给认知/资源/氛围缓冲。Checkpoint、宝箱、景观房。</summary>
        Reward,

        /// <summary>锚点节点：帮助玩家建立脑内地图。明显地标、特殊 CameraTrigger、独特 Biome。</summary>
        Anchor,

        /// <summary>回路节点：把单向推进转化成结构性掌控。解锁门、反向通路、回旧 Checkpoint 的捷径。</summary>
        Loop,

        /// <summary>枢纽节点：多条路径的交汇点，提供导航选择。通常是大区域的中心。</summary>
        Hub,

        /// <summary>门槛节点：章节边界，进入后世界发生不可逆变化。Boss 前厅、关键剧情触发点。</summary>
        Threshold,

        /// <summary>安全节点：完全没有敌意的休息区。Safe Room、商店。</summary>
        Safe,

        /// <summary>Boss 节点：Boss 竞技场，独立演出流程。</summary>
        Boss
    }
}
