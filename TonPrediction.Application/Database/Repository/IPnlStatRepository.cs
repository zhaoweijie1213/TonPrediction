using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.SqlSugar;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Enums;

namespace TonPrediction.Application.Database.Repository;

/// <summary>
/// 用户盈亏统计仓库接口。
/// </summary>
public interface IPnlStatRepository : IBaseRepository<PnlStatEntity>, ITransientDependency
{
    /// <summary>
    /// 根据地址获取统计记录。
    /// </summary>
    /// <param name="symbol">币种符号。</param>
    /// <param name="address">用户地址。</param>
    Task<PnlStatEntity?> GetByAddressAsync(string symbol, string address);

    /// <summary>
    /// 分页获取排行榜数据。
    /// </summary>
    /// <param name="symbol">币种符号。</param>
    /// <param name="rankBy">排序字段。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">分页大小。</param>
    Task<List<PnlStatEntity>> GetPagedAsync(string symbol, RankByType rankBy, int page, int pageSize);

    /// <summary>
    /// 获取指定地址的排名。
    /// </summary>
    /// <param name="symbol">币种符号。</param>
    /// <param name="address">用户地址。</param>
    /// <param name="rankBy">排序字段。</param>
    Task<int> GetRankAsync(string symbol, string address, RankByType rankBy);

    /// <summary>
    /// 模糊搜索地址。
    /// </summary>
    /// <param name="symbol">币种符号。</param>
    /// <param name="keyword">地址关键字。</param>
    /// <param name="limit">结果数量。</param>
    Task<List<string>> SearchAddressAsync(string symbol, string keyword, int limit);
}
