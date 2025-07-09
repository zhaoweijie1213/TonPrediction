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
        /// 下注进行中。
        /// </summary>
        Betting = 1,

        /// <summary>
        /// 已锁价，等待收盘。
        /// </summary>
        Live = 2,

        /// <summary>
        /// 结算中。
        /// </summary>
        Calculating = 3,

        /// <summary>
        /// 已完成，可领取奖励。
        /// </summary>
        Completed = 4
    }
}
