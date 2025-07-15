using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TonPrediction.Application.Config
{
    /// <summary>
    /// 预测相关配置。
    /// </summary>
    public class PredictionConfig
    {
        /// <summary>
        /// 允许交易在锁仓时间后被接受的容错秒数
        /// </summary>
        public int BetTimeToleranceSeconds { get; set; } = 5;

        /// <summary>
        /// 回合间隔秒数
        /// </summary>
        public int RoundIntervalSeconds { get; set; } = 60;
    }
}
