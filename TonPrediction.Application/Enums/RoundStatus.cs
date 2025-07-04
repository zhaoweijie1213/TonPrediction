namespace TonPrediction.Application.Enums
{
    /// <summary>
    /// 回合状态。
    /// </summary>
    public enum RoundStatus
    {
        /// <summary>
        /// 即将开始。
        /// </summary>
        Upcoming = 0,
        /// <summary>
        /// 正在进行。
        /// </summary>
        Live = 1,
        /// <summary>
        /// 已锁定，不可下注。
        /// </summary>
        Locked = 2,
        /// <summary>
        /// 已结束。
        /// </summary>
        Ended = 3
    }
}
