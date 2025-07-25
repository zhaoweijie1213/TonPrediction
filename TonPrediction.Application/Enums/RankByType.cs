using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TonPrediction.Application.Enums
{
    /// <summary>
    /// 
    /// </summary>
    public enum RankByType
    {
        /// <summary>
        /// 回合数。
        /// </summary>
        Rounds,

        /// <summary>
        /// 净收益
        /// </summary>
        NetProfit,

        /// <summary>
        /// 累计下注
        /// </summary>
        TotalBet,

        /// <summary>
        /// 胜率
        /// </summary>
        WinRate,

        /// <summary>
        /// 总奖金
        /// </summary>
        TotalReward
    }
}
