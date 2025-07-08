using TonPrediction.Application.Database.Entities;
using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.SqlSugar;

namespace TonPrediction.Application.Database.Repository
{
    /// <summary>
    /// 价格快照仓库接口。
    /// </summary>
    public interface IPriceSnapshotRepository : IBaseRepository<PriceSnapshotEntity>, ITransientDependency
    {
        /// <summary>
        /// 获取指定时间之后的价格快照。
        /// </summary>
        /// <param name="since">起始时间。</param>
        /// <param name="symbol"></param>
        /// <param name="ct">取消令牌。</param>
        Task<List<PriceSnapshotEntity>> GetSinceAsync(
            string symbol,
            DateTime since,
            CancellationToken ct = default);
    }
}
