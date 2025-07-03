using SqlSugar;
using TonPrediction.Application.Enums;

namespace TonPrediction.Application.Database.Entities
{
    /// <summary>
    /// 表示用户在某回合中的下注记录。
    /// </summary>
    [SugarTable("bet")]
    public class BetEntity
    {
        /// <summary>
        /// 主键编号。
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// 通常翻译成 “回合编号” 或 “期次”，用来唯一标识一轮竞猜的完整生命周期
        /// </summary>
        [SugarColumn(ColumnName = "epoch_id")]
        public long Epoch { get; set; }

        /// <summary>
        /// 用户地址。
        /// </summary>
        [SugarColumn(ColumnName = "user_address")]
        public string UserAddress { get; set; } = string.Empty;

        /// <summary>
        /// 下注金额。
        /// </summary>
        [SugarColumn(ColumnName = "amount", ColumnDataType = "decimal(18,8)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 下注方向。
        /// </summary>
        [SugarColumn(ColumnName = "position")]
        public Position Position { get; set; }

        /// <summary>
        /// 是否已领取奖励。
        /// </summary>
        [SugarColumn(ColumnName = "claimed")]
        public bool Claimed { get; set; }

        /// <summary>
        /// 奖励金额。
        /// </summary>
        [SugarColumn(ColumnName = "reward", ColumnDataType = "decimal(18,8)")]
        public decimal Reward { get; set; }
    }
}
