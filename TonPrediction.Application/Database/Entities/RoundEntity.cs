using System;
using SqlSugar;
using TonPrediction.Application.Enums;

namespace TonPrediction.Application.Database.Entities
{
    /// <summary>
    /// 表示链上预测游戏的回合记录。
    /// </summary>
    [SugarTable("round")]
    public class RoundEntity
    {

        /// <summary>
        /// 主键编号，通常使用时间戳生成，保证全局唯一。
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
        public long Id { get; set; }

        /// <summary>
        /// 期次，从 1 开始按币种独立递增。
        /// </summary>
        [SugarColumn(ColumnName = "epoch")]
        public long Epoch { get; set; }

        /// <summary>
        /// 预测币种符号，如 ton、btc、eth。
        /// </summary>
        [SugarColumn(ColumnName = "symbol", Length = 16)]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// 回合开始时间。
        /// </summary>
        [SugarColumn(ColumnName = "start_time")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 锁仓时间。
        /// </summary>
        [SugarColumn(ColumnName = "lock_time")]
        public DateTime LockTime { get; set; }

        /// <summary>
        /// 回合结束时间。
        /// </summary>
        [SugarColumn(ColumnName = "close_time")]
        public DateTime CloseTime { get; set; }

        /// <summary>
        /// 锁仓价格。
        /// </summary>
        [SugarColumn(ColumnName = "lock_price", ColumnDataType = "decimal(18,8)")]
        public decimal LockPrice { get; set; }

        /// <summary>
        /// 锁定的预言机 ID。
        /// </summary>
        [SugarColumn(ColumnName = "lock_oracle_id")]
        public long LockOracleId { get; set; }

        /// <summary>
        /// 预言机 ID，用于获取结束价格。
        /// </summary>
        [SugarColumn(ColumnName = "close_oracle_id")]
        public long CloseOracleId { get; set; }

        /// <summary>
        /// 结束价格。
        /// </summary>
        [SugarColumn(ColumnName = "close_price", ColumnDataType = "decimal(18,8)")]
        public decimal ClosePrice { get; set; }

        /// <summary>
        /// 回合状态。
        /// </summary>
        [SugarColumn(ColumnName = "status")]
        public RoundStatus Status { get; set; }

        /// <summary>
        /// 本回合的下注总金额。
        /// </summary>
        [SugarColumn(ColumnName = "total_amount", ColumnDataType = "bigint")]
        public long TotalAmount { get; set; }

        /// <summary>
        /// 押涨总金额。
        /// </summary>
        [SugarColumn(ColumnName = "bull_amount", ColumnDataType = "bigint")]
        public long BullAmount { get; set; }

        /// <summary>
        /// 押跌总金额。
        /// </summary>
        [SugarColumn(ColumnName = "bear_amount", ColumnDataType = "bigint")]
        public long BearAmount { get; set; }

        /// <summary>
        /// 获胜方。
        /// </summary>
        [SugarColumn(ColumnName = "winner_side", IsNullable = true)]
        public Position? WinnerSide { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [SugarColumn(ColumnName = "reward_base_cal_amount", ColumnDataType = "bigint")]
        public long RewardBaseCalAmount { get; set; }

        /// <summary>
        /// 可分配奖金池。
        /// </summary>
        [SugarColumn(ColumnName = "reward_amount", ColumnDataType = "bigint")]
        public long RewardAmount { get; set; }
    }
}
