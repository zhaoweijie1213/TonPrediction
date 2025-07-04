using QYQ.Base.Common.IOCExtensions;

namespace TonPrediction.Application.Services
{
    /// <summary>
    /// 提供价格数据的服务接口。
    /// </summary>
    public interface IPriceService : ITransientDependency
    {
        /// <summary>
        /// 获取当前价格。
        /// </summary>
        /// <param name="token">取消任务标记。</param>
        /// <returns>当前价格。</returns>
        Task<decimal> GetCurrentPriceAsync(CancellationToken token);
    }
}
