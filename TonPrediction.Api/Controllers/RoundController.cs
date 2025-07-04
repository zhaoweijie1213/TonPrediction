using Microsoft.AspNetCore.Mvc;
using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Api.Controllers;

/// <summary>
/// 回合相关接口。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RoundController(IRoundService roundService) : ControllerBase
{
    private readonly IRoundService _roundService = roundService;

    /// <summary>
    /// 获取历史回合列表。
    /// </summary>
    [HttpGet("history")]
    public async Task<ApiResult<List<RoundHistoryOutput>>> GetHistoryAsync(
        [FromQuery] string symbol = "ton",
        [FromQuery] int limit = 3)
    {
        var result = await _roundService.GetHistoryAsync(symbol, limit);
        var api = new ApiResult<List<RoundHistoryOutput>>();
        api.SetRsult(ApiResultCode.Success, result);
        return api;
    }

    /// <summary>
    /// 获取即将开始的回合。
    /// </summary>
    [HttpGet("upcoming")]
    public async Task<ApiResult<List<UpcomingRoundOutput>>> GetUpcomingAsync(
        [FromQuery] string symbol = "ton")
    {
        var list = await _roundService.GetUpcomingAsync(symbol);
        var api = new ApiResult<List<UpcomingRoundOutput>>();
        api.SetRsult(ApiResultCode.Success, list);
        return api;
    }
}
