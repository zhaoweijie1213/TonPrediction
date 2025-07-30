using QYQ.Base.Common.IOCExtensions;

namespace TonPrediction.Application.Services.Interface
{
    /// <summary>
    /// 提供价格数据的服务接口。
    /// </summary>
    public interface IPriceService
    {
        /// <summary>
        /// 获取某币种在指定法币中的最新价格。
        /// </summary>
        /// <param name="symbol">币种符号，小写，如 "ton" 或 "btc"。</param>
        /// <param name="vsCurrency">法币符号，默认为 usd。</param>
        /// <param name="ct">取消任务标记。</param>
        /// <returns>价格结果。</returns>
        Task<PriceResult> GetAsync(
            string symbol,
            string vsCurrency = "usd",
            CancellationToken ct = default);

        /// <summary>
        /// 获取最近时间段内的价格列表，默认30分钟，每分钟一个数据点。
        /// </summary>
        /// <param name="symbol">币种符号，小写。</param>
        /// <param name="vsCurrency">法币符号，默认为 usd。</param>
        /// <param name="minutes">回溯分钟数。</param>
        /// <param name="ct">取消任务标记。</param>
        /// <returns>价格结果列表。</returns>
        Task<IReadOnlyList<PriceResult>> GetRecentPricesAsync(
            string symbol,
            string vsCurrency = "usd",
            int minutes = 30,
            CancellationToken ct = default);
    }
}
