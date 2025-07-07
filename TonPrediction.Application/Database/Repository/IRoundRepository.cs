using TonPrediction.Application.Database.Entities;
using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TonPrediction.Application.Database.Repository
{
    /// <summary>
    /// 每回合的仓库接口
    /// </summary>
    public interface IRoundRepository : IBaseRepository<RoundEntity>, ITransientDependency
    {
        /// <summary>
        /// 获取最新一轮记录。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        Task<RoundEntity?> GetLatestAsync(
            string symbol,
            CancellationToken ct = default);

        /// <summary>
        /// 获取当前进行中的回合。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        Task<RoundEntity?> GetCurrentLiveAsync(
            string symbol,
            CancellationToken ct = default);

        /// <summary>
        /// 获取当前锁定中的回合。
        /// </summary>
        /// <param name="symbol">币种符号。</param>
        /// <param name="ct">取消令牌。</param>
        Task<RoundEntity?> GetCurrentLockedAsync(
            string symbol,
            CancellationToken ct = default);

        /// <summary>
        /// 获取最近结束的若干回合。
        /// </summary>
        /// <param name="limit">限制数量。</param>
        /// <param name="ct">取消令牌。</param>
        Task<List<RoundEntity>> GetEndedAsync(
            string symbol,
            int limit,
            CancellationToken ct = default);

        /// <summary>
        /// 根据编号批量查询回合。
        /// </summary>
        /// <param name="roundIds">回合序号集合。</param>
        /// <param name="ct">取消令牌。</param>
        Task<List<RoundEntity>> GetByRoundIdsAsync(
            long[] roundIds,
            CancellationToken ct = default);
    }
}
