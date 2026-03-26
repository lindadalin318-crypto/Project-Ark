namespace ProjectArk.Level
{
    /// <summary>
    /// 门过渡的仪式感等级。不同等级对应不同的过渡演出时长、VFX、镜头行为。
    /// DoorTransitionController 根据此值选择不同的过渡行为。
    /// </summary>
    public enum TransitionCeremony
    {
        /// <summary>无过渡，瞬间切换（同房间内的隐藏通道等）。</summary>
        None,

        /// <summary>标准过渡：短淡黑（0.3s），适用于大部分普通门。</summary>
        Standard,

        /// <summary>层间过渡：长淡黑（0.5s）+ 下坠粒子 + 环境音切换。</summary>
        Layer,

        /// <summary>Boss 门过渡：特写演出 + 禁用玩家 + 独立音效。</summary>
        Boss,

        /// <summary>重型门过渡：多段联动 + 震屏 + 粒子 + 长演出。</summary>
        Heavy
    }
}
