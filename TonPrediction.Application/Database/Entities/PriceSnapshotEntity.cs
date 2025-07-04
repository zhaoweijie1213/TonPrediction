using System;
using SqlSugar;

namespace TonPrediction.Application.Database.Entities
{
    /// <summary>
    /// 价格快照记录。
    /// </summary>
    [SugarTable("price_snapshot")]
    public class PriceSnapshotEntity
    {
        /// <summary>
        /// 价格所属币种符号。
        /// </summary>
        [SugarColumn(ColumnName = "symbol", Length = 16)]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// 主键编号。
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// 时间戳。
        /// </summary>
        [SugarColumn(ColumnName = "timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 当前价格。
        /// </summary>
        [SugarColumn(ColumnName = "price", ColumnDataType = "decimal(18,8)")]
        public decimal Price { get; set; }
    }
}
