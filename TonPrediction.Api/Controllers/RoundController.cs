using Microsoft.AspNetCore.Mvc;
using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Services;
using TonPrediction.Application.Output;

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
    public async Task<ApiResult<List<RoundHistoryOutput>>> GetHistoryAsync([FromQuery] int limit = 3)
    {
        var result = await _roundService.GetHistoryAsync(limit);
        var api = new ApiResult<List<RoundHistoryOutput>>();
        api.SetRsult(ApiResultCode.Success, result);
        return api;
    }

    /// <summary>
    /// 获取即将开始的回合。
    /// </summary>
    [HttpGet("upcoming")]
    public async Task<ApiResult<List<UpcomingRoundOutput>>> GetUpcomingAsync()
    {
        var list = await _roundService.GetUpcomingAsync();
        var api = new ApiResult<List<UpcomingRoundOutput>>();
        api.SetRsult(ApiResultCode.Success, list);
        return api;
    }
}
