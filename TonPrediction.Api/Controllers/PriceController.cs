using Microsoft.AspNetCore.Mvc;
using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Output;

namespace TonPrediction.Api.Controllers;

/// <summary>
/// 价格相关接口。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PriceController(IPriceSnapshotRepository repo) : ControllerBase
{
    private readonly IPriceSnapshotRepository _repo = repo;

    /// <summary>
    /// 获取价格走势图数据。
    /// </summary>
    [HttpGet("chart")]
    public async Task<ApiResult<ChartDataOutput>> GetChartAsync([FromQuery] string symbol = "ton")
    {
        var since = DateTime.UtcNow.AddMinutes(-10);
        var list = await _repo.GetSinceAsync(symbol, since);
        var output = new ChartDataOutput
        {
            Timestamps = list.Select(d => new DateTimeOffset(d.Timestamp).ToUnixTimeSeconds()).ToArray(),
            Prices = list.Select(d => d.Price.ToString("F8")).ToArray()
        };
        var api = new ApiResult<ChartDataOutput>();
        api.SetRsult(ApiResultCode.Success, output);
        return api;
    }
}
