using SqlSugar;

namespace TonPrediction.Application.Database.Entities
{
    /// <summary>
    /// 用户领取奖励记录。
    /// </summary>
    [SugarTable("claim")]
    public class ClaimEntity
    {
        /// <summary>
        /// 主键编号。
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// 回合编号。
        /// </summary>
        [SugarColumn(ColumnName = "round_id")]
        public long RoundId { get; set; }

        /// <summary>
        /// 用户地址。
        /// </summary>
        [SugarColumn(ColumnName = "user_address")]
        public string UserAddress { get; set; } = string.Empty;

        /// <summary>
        /// 奖励金额。
        /// </summary>
        [SugarColumn(ColumnName = "reward", ColumnDataType = "decimal(18,8)")]
        public decimal Reward { get; set; }

        /// <summary>
        /// 交易哈希。
        /// </summary>
        [SugarColumn(ColumnName = "tx_hash")]
        public string TxHash { get; set; } = string.Empty;
    }
}
