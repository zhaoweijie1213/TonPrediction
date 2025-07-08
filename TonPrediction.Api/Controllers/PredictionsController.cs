using Microsoft.AspNetCore.Mvc;
using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Api.Controllers;

/// <summary>
/// 下注记录与盈亏接口。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PredictionsController(IPredictionService predictionService) : ControllerBase
{
    private readonly IPredictionService _predictionService = predictionService;

    /// <summary>
    /// 分页获取下注记录。
    /// </summary>
    [HttpGet("round")]
    public async Task<ApiResult<List<BetRecordOutput>>> GetRoundAsync(
        [FromQuery] string address,
        [FromQuery] string status = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return await _predictionService.GetRecordsAsync(address, status, page, pageSize);
    }

    /// <summary>
    /// 获取盈亏汇总。
    /// </summary>
    [HttpGet("pnl")]
    public async Task<ApiResult<PnlOutput>> GetPnlAsync([FromQuery] string address)
    {
        return await _predictionService.GetPnlAsync(address);
    }
}
