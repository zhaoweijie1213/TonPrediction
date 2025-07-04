namespace TonPrediction.Application.Services
{
    /// <summary>
    /// 表示价格查询结果。
    /// </summary>
    /// <param name="Symbol">查询的币种符号。</param>
    /// <param name="VsCurrency">法币符号。</param>
    /// <param name="Price">对应价格。</param>
    /// <param name="Timestamp">查询时间。</param>
    public record PriceResult(
        string Symbol,
        string VsCurrency,
        decimal Price,
        DateTimeOffset Timestamp);
}
