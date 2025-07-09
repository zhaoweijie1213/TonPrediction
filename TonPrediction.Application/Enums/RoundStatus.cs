namespace TonPrediction.Application.Enums
{
    /// <summary>
    /// 回合状态。
    /// </summary>
    public enum RoundStatus
    {
        ///// <summary>
        ///// 预生成、未开始
        ///// </summary>
        //Upcoming = 0,

        /// <summary>
        /// 未开始,正在下注
        /// </summary>
        Betting = 1,

        /// <summary>
        /// 已锁价，等待收盘
        /// start
        /// </summary>
        Locked = 2,

        /// <summary>
        /// 拿到 closePrice，结算中
        /// </summary>
        Calculating = 3,

        /// <summary>
        /// 结算完，玩家可 claim
        /// </summary>
        Completed = 4,

        /// <summary>
        /// 平盘 / 预言机异常 / 强行退款
        /// </summary>
        Cancelled = 5
    }
}
