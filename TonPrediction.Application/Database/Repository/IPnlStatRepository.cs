using TonPrediction.Application.Database.Entities;
using QYQ.Base.SqlSugar;
using QYQ.Base.Common.IOCExtensions;

namespace TonPrediction.Application.Database.Repository;

/// <summary>
/// 用户盈亏统计仓库接口。
/// </summary>
public interface IPnlStatRepository : IBaseRepository<PnlStatEntity>, ITransientDependency
{
    /// <summary>
    /// 根据地址获取统计记录。
    /// </summary>
    /// <param name="address">用户地址。</param>
    Task<PnlStatEntity?> GetByAddressAsync(string address);

    /// <summary>
    /// 分页获取排行榜数据。
    /// </summary>
    /// <param name="rankBy">排序字段。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">分页大小。</param>
    Task<List<PnlStatEntity>> GetPagedAsync(string rankBy, int page, int pageSize);

    /// <summary>
    /// 获取指定地址的排名。
    /// </summary>
    /// <param name="address">用户地址。</param>
    /// <param name="rankBy">排序字段。</param>
    Task<int> GetRankAsync(string address, string rankBy);
}
