using Microsoft.AspNetCore.Mvc;
using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Input;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Api.Controllers;

/// <summary>
/// 下注上报接口。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BetController(IBetService betService) : ControllerBase
{
    private readonly IBetService _betService = betService;

    /// <summary>
    /// 验证指定回合是否可以下注。
    /// </summary>
    [HttpGet("verify")]
    public async Task<ApiResult<bool>> VerifyAsync([FromQuery] long roundId)
    {
        return await _betService.VerifyAsync(roundId);
    }

    /// <summary>
    /// 用户提交交易 BOC 以记录下注。
    /// </summary>
    [HttpPost("report")]
    public async Task<ApiResult<bool>> ReportAsync([FromBody] BocInput input)
    {
        return await _betService.ReportAsync(input.Boc);
    }
}
