﻿using TonPrediction.Application.Database.Entities;
using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TonPrediction.Application.Database.Repository
{
    /// <summary>
    /// 下注仓库接口
    /// </summary>
    public interface IBetRepository : IBaseRepository<BetEntity>, ITransientDependency
    {
        /// <summary>
        /// 获取指定地址的分页下注记录。
        /// </summary>
        /// <param name="address">用户地址。</param>
        /// <param name="predicate">额外的筛选条件。</param>
        /// <param name="page">页码。</param>
        /// <param name="pageSize">每页数量。</param>
        /// <param name="ct">取消令牌。</param>
        Task<List<BetEntity>> GetPagedByAddressAsync(
            string address,
            Expression<Func<BetEntity, bool>>? predicate,
            int page,
            int pageSize);

        /// <summary>
        /// 获取指定地址的全部下注记录。
        /// </summary>
        /// <param name="address">用户地址。</param>
        /// <param name="ct">取消令牌。</param>
        Task<List<BetEntity>> GetByAddressAsync(
            string address,
            CancellationToken ct = default);

        /// <summary>
        /// 获取指定回合的全部下注记录。
        /// </summary>
        /// <param name="roundId">回合编号。</param>
        /// <param name="ct">取消令牌。</param>
        Task<List<BetEntity>> GetByRoundAsync(
            long roundId,
            CancellationToken ct = default);

        /// <summary>
        /// 根据用户地址和回合查询下注记录。
        /// </summary>
        /// <param name="address">用户地址。</param>
        /// <param name="roundId">回合编号。</param>
        /// <param name="ct">取消令牌。</param>
        Task<BetEntity?> GetByAddressAndRoundAsync(
            string address,
            long roundId,
            CancellationToken ct = default);

        /// <summary>
        /// 根据地址和多个回合查询下注记录。
        /// </summary>
        /// <param name="address">用户地址。</param>
        /// <param name="roundIds">回合编号集合。</param>
        /// <param name="ct">取消令牌。</param>
        Task<List<BetEntity>> GetByAddressAndRoundsAsync(
            string address,
            long[] roundIds,
            CancellationToken ct = default);

        /// <summary>
        /// 根据交易哈希查询下注记录。
        /// </summary>
        /// <param name="txHash">交易哈希。</param>
        Task<BetEntity?> GetByTxHashAsync(string txHash);

        /// <summary>
        /// 获取用户下注回合信息
        /// </summary>
        /// <returns></returns>
        Task<BetEntity> GetByRoundAndUserAsync(long roundId, string userAddress);
    }
}
