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
        [SugarColumn(ColumnName = "round_id")]
        public long RoundId { get; set; }

        /// <summary>
        /// 用户地址。
        /// </summary>
        [SugarColumn(ColumnName = "user_address", UniqueGroupNameList = new[] { "uq_address_lt" })]
        public string UserAddress { get; set; } = string.Empty;

        /// <summary>
        /// 下注金额。
        /// </summary>
        [SugarColumn(ColumnName = "amount", ColumnDataType = "bigint")]
        public long Amount { get; set; }

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
        [SugarColumn(ColumnName = "reward", ColumnDataType = "bigint")]
        public long Reward { get; set; }

        /// <summary>
        /// 手续费
        /// </summary>
        [SugarColumn(ColumnName = "treasury_fee", ColumnDataType = "bigint")]
        public long TreasuryFee { get; set; }

        /// <summary>
        /// 下注交易哈希，唯一索引避免重复插入。
        /// </summary>
        [SugarColumn(ColumnName = "tx_hash", UniqueGroupNameList = new[] { "uq_tx_hash" })]
        public string TxHash { get; set; } = string.Empty;

        /// <summary>
        /// 下注状态。
        /// </summary>
        [SugarColumn(ColumnName = "status")]
        public BetStatus Status { get; set; }

        /// <summary>
        /// 交易账户逻辑时间。
        /// </summary>
        [SugarColumn(ColumnName = "lt", UniqueGroupNameList = new[] { "uq_address_lt" })]
        public ulong Lt { get; set; }
    }
}
