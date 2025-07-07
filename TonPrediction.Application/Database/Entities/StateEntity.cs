using SqlSugar;

namespace TonPrediction.Application.Database.Entities
{
    /// <summary>
    /// 用于存储应用状态的简单键值实体。
    /// </summary>
    [SugarTable("state")]
    public class StateEntity
    {
        /// <summary>
        /// 状态键。
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, ColumnName = "key", Length = 64)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 状态值。
        /// </summary>
        [SugarColumn(ColumnName = "value", Length = 256)]
        public string Value { get; set; } = string.Empty;
    }
}
