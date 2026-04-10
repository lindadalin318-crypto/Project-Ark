namespace ProjectArk.Level
{
    /// <summary>
    /// 房间的主体验类型。
    /// 只回答“玩家进入这个房间后，主要在做什么？”。
    /// 结构身份（如 Anchor / Loop / Hub / Threshold）不再放在这里，后续应由 WorldGraph 标签或附加元数据承载。
    /// 当前仓库只允许新六类枚举值，不再保留旧模型的兼容序列化编号。
    /// </summary>
    public enum RoomNodeType
    {
        /// <summary>过路房。主体验是通过与连接，可带少量敌人或机关，但不承载强事件。</summary>
        Transit = 0,

        /// <summary>开放战斗房。主体验是边移动边打，流程不断。</summary>
        Combat = 1,

        /// <summary>竞技场。主体验是锁门清场或波次验证。</summary>
        Arena = 2,

        /// <summary>回报房。主体验是获得资源、剧情或探索回报。</summary>
        Reward = 3,

        /// <summary>安全房。主体验是休整，没有持续敌意内容。</summary>
        Safe = 4,

        /// <summary>Boss 房。主体验是完整 Boss 战流程。</summary>
        Boss = 5
    }
}
