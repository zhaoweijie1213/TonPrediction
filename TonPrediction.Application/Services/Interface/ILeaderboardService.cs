using QYQ.Base.Common.ApiResult;
using QYQ.Base.Common.IOCExtensions;
using TonPrediction.Application.Enums;
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
        RankByType rankBy = RankByType.NetProfit,
        int page = 1,
        int pageSize = 10,
        string? address = null);

    /// <summary>
    /// 获取指定地址的排行榜信息。
    /// </summary>
    /// <param name="address">用户地址。</param>
    /// <param name="symbol">币种符号。</param>
    /// <param name="rankBy">排序字段。</param>
    Task<ApiResult<LeaderboardItemOutput?>> GetByAddressAsync(
        string address,
        string symbol = "ton",
        RankByType rankBy = RankByType.NetProfit);

    /// <summary>
    /// 模糊搜索地址。
    /// </summary>
    /// <param name="keyword">地址关键字。</param>
    /// <param name="symbol">币种符号。</param>
    /// <param name="limit">返回数量限制。</param>
    Task<ApiResult<AddressListOutput>> SearchAddressAsync(
        string keyword,
        string symbol = "ton",
        int limit = 10);
}
