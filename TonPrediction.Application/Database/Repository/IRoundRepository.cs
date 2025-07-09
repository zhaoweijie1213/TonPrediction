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
        /// 获取创世回合信息
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public Task<RoundEntity?> GetGenesisRoundAsync(string symbol);

        /// <summary>
        /// 获取最新一轮记录。
        /// </summary>
        /// <param name="symbol"></param>
        Task<RoundEntity?> GetLatestAsync(string symbol);

        /// <summary>
        /// 获取当前下注中的回合。
        /// </summary>
        /// <param name="symbol"></param>
        Task<RoundEntity?> GetCurrentBettingAsync(string symbol);

        ///// <summary>
        ///// 获取下一回合记录（状态为 Upcoming）。
        ///// </summary>
        ///// <param name="symbol">币种符号。</param>
        //Task<RoundEntity?> GetUpcomingAsync(string symbol);

        /// <summary>
        /// 获取当前锁定中的回合。
        /// </summary>
        /// <param name="symbol">币种符号。</param>
        Task<RoundEntity?> GetCurrentLockedAsync(string symbol);

        /// <summary>
        /// 获取最近结束的若干回合。
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="limit">限制数量。</param>
        Task<List<RoundEntity>> GetEndedAsync(string symbol, int limit);

        /// <summary>
        /// 获取最近若干回合（不限状态）。
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="limit">限制数量。</param>
        Task<List<RoundEntity>> GetRecentAsync(string symbol, int limit);

        /// <summary>
        /// 获取回合信息通过回合编号。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<RoundEntity> GetByIdAsync(long id);

        /// <summary>
        /// 根据编号批量查询回合。
        /// </summary>
        /// <param name="roundIds">回合序号集合。</param>
        Task<List<RoundEntity>> GetByRoundIdsAsync(long[] roundIds);

        /// <summary>
        /// 获取指定币种和回合编号的回合信息。
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="epoch"></param>
        /// <returns></returns>
        public Task<RoundEntity> GetByEpochAsync(string symbol, long epoch);
    }
}
