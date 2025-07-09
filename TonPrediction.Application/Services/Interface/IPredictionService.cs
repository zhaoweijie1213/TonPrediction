using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Output;

namespace TonPrediction.Application.Services.Interface;

/// <summary>
/// 预测记录查询业务接口。
/// </summary>
public interface IPredictionService : ITransientDependency
{
    /// <summary>
    /// 分页获取指定地址的下注记录。
    /// </summary>
    /// <param name="address">用户地址。</param>
    /// <param name="status">记录状态：all/claimed/unclaimed。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    /// <param name="ct">取消任务标记。</param>
    /// <returns>回合及下注信息列表。</returns>
    Task<ApiResult<List<RoundUserBetOutput>>> GetRecordsAsync(
        string address,
        string status = "all",
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    /// <summary>
    /// 获取指定地址的盈亏汇总。
    /// </summary>
    /// <param name="symbol">币种符号。</param>
    /// <param name="address">用户地址。</param>
    /// <returns>盈亏信息。</returns>
    Task<ApiResult<PnlOutput>> GetPnlAsync(string symbol, string address);
}
