using Microsoft.AspNetCore.Mvc;
using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Application.Output;
using TonPrediction.Application.Extensions;

namespace TonPrediction.Api.Controllers;

/// <summary>
/// 价格相关接口。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PriceController(IPriceService priceService) : ControllerBase
{
    private readonly IPriceService _priceService = priceService;

    /// <summary>
    /// 获取价格走势图数据。
    /// </summary>
    [HttpGet("chart")]
    public async Task<ApiResult<ChartDataOutput>> GetChartAsync([FromQuery] string symbol = "ton")
    {
        var list = await _priceService.GetRecentPricesAsync(symbol, "usd");
        var output = new ChartDataOutput
        {
            Timestamps = list.Select(d => d.Timestamp.ToUnixTimeSeconds()).ToArray(),
            Prices = list.Select(d => d.Price.ToAmountString()).ToArray()
        };
        var api = new ApiResult<ChartDataOutput>();
        api.SetRsult(ApiResultCode.Success, output);
        return api;
    }
}
