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
    [Obsolete("使用recent接口")]
    public async Task<ApiResult<List<RoundHistoryOutput>>> GetHistoryAsync([FromQuery] string symbol = "ton", [FromQuery] int limit = 3)
    {
        return await _roundService.GetHistoryAsync(symbol, limit);
    }

    /// <summary>
    /// 获取即将开始回合。
    /// </summary>
    [HttpGet("upcoming")]
    [Obsolete("使用recent接口")]
    public async Task<ApiResult<UpcomingRoundOutput>> GetUpcomingAsync([FromQuery] string symbol = "ton")
    {
        return await _roundService.GetUpcomingAsync(symbol);
    }

    /// <summary>
    /// 获取最近回合及用户下注信息。
    /// </summary>
    [HttpGet("recent")]
    public async Task<ApiResult<List<RoundUserBetOutput>>> GetRecentAsync([FromQuery] string? address, [FromQuery] string symbol = "ton", [FromQuery] int limit = 5)
    {
        return await _roundService.GetRecentAsync(address, symbol, limit);
    }
}
