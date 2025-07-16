using SqlSugar;
using SqlSugar.DbConvert;
using TonPrediction.Application.Enums;

namespace TonPrediction.Application.Database.Entities
{
    /// <summary>
    /// 领奖与退款交易记录。
    /// </summary>
    [SugarTable("transaction")]
    public class TransactionEntity
    {
        /// <summary>
        /// 主键编号。
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// 下注记录编号。
        /// </summary>
        [SugarColumn(ColumnName = "bet_id")]
        public int BetId { get; set; }

        /// <summary>
        /// 用户地址。
        /// </summary>
        [SugarColumn(ColumnName = "user_address")]
        public string UserAddress { get; set; } = string.Empty;

        /// <summary>
        /// 交易金额。
        /// </summary>
        [SugarColumn(ColumnName = "amount", ColumnDataType = "bigint")]
        public long Amount { get; set; }

        /// <summary>
        /// 交易哈希。
        /// </summary>
        [SugarColumn(ColumnName = "tx_hash")]
        public string TxHash { get; set; } = string.Empty;

        /// <summary>
        /// 交易状态。
        /// </summary>
        [SugarColumn(ColumnName = "status", ColumnDataType = "varchar(20)", SqlParameterDbType = typeof(EnumToStringConvert))]
        public ClaimStatus Status { get; set; }

        /// <summary>
        /// 交易账户逻辑时间。
        /// </summary>
        [SugarColumn(ColumnName = "lt")]
        public ulong Lt { get; set; }

        /// <summary>
        /// 交易时间（UTC）。
        /// </summary>
        [SugarColumn(ColumnName = "timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
