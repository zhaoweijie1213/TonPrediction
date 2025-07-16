using QYQ.Base.Common.ApiResult;
using QYQ.Base.Common.IOCExtensions;
using TonPrediction.Application.Output;

namespace TonPrediction.Application.Services.Interface;

/// <summary>
/// 排行榜业务接口。
/// </summary>
public interface ILeaderboardService : ITransientDependency
{
    /// <summary>
    /// 获取排行榜列表。
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="rankBy">排序字段。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">分页大小。</param>
    /// <param name="address">可选地址。</param>
    Task<ApiResult<LeaderboardOutput>> GetListAsync(
        string symbol = "ton",
        string rankBy = "netProfit",
        int page = 1,
        int pageSize = 10,
        string? address = null);
}
