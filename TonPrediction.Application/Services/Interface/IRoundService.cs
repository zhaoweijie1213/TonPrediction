using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Output;

namespace TonPrediction.Application.Services.Interface;

/// <summary>
/// 回合信息查询业务接口。
/// </summary>
public interface IRoundService : ITransientDependency
{
    /// <summary>
    /// 获取历史回合列表。
    /// </summary>
    /// <param name="limit">最大返回数量。</param>
    /// <param name="symbol"></param>
    /// <param name="ct">取消任务标记。</param>
    /// <returns>历史回合集合。</returns>
    Task<ApiResult<List<RoundHistoryOutput>>> GetHistoryAsync(
        string symbol = "ton",
        int limit = 3,
        CancellationToken ct = default);

    /// <summary>
    /// 获取下一回合的时间信息。
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="ct">取消任务标记。</param>
    /// <returns>下一回合时间。</returns>
    Task<ApiResult<UpcomingRoundOutput>> GetUpcomingAsync(
        string symbol = "ton",
        CancellationToken ct = default);

    /// <summary>
    /// 获取最近回合以及用户下注信息。
    /// </summary>
    /// <param name="address">用户地址。</param>
    /// <param name="symbol">币种符号。</param>
    /// <param name="limit">返回数量限制。</param>
    /// <param name="ct">取消任务标记。</param>
    Task<ApiResult<List<RoundUserBetOutput>>> GetRecentAsync(
        string address,
        string symbol = "ton",
        int limit = 5,
        CancellationToken ct = default);
}
