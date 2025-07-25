using Microsoft.AspNetCore.Mvc;
using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Api.Controllers;

/// <summary>
/// 排行榜接口。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LeaderboardController(ILeaderboardService service) : ControllerBase
{
    private readonly ILeaderboardService _service = service;

    /// <summary>
    /// 获取排行榜列表。
    /// </summary>
    [HttpGet("list")]
    public async Task<ApiResult<LeaderboardOutput>> GetListAsync(
        [FromQuery] string symbol = "ton",
        [FromQuery] RankByType rankBy = RankByType.NetProfit,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? address = null)
    {
        return await _service.GetListAsync(symbol, rankBy, page, pageSize, address);
    }

    /// <summary>
    /// 获取指定地址的排行榜信息。
    /// </summary>
    [HttpGet("address")]
    public async Task<ApiResult<LeaderboardItemOutput?>> GetByAddressAsync(
        [FromQuery] string address,
        [FromQuery] string symbol = "ton",
        [FromQuery] RankByType rankBy = RankByType.NetProfit)
    {
        return await _service.GetByAddressAsync(address, symbol, rankBy);
    }
}
