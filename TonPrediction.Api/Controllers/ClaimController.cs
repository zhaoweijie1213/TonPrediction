using Microsoft.AspNetCore.Mvc;
using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Input;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Api.Controllers;

/// <summary>
/// 领奖接口。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ClaimController(IClaimService claimService) : ControllerBase
{
    private readonly IClaimService _claimService = claimService;

    /// <summary>
    /// 领取指定回合奖励。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<ClaimOutput?>> ClaimAsync([FromBody] ClaimInput input)
    {
        return await _claimService.ClaimAsync(input);
    }
}
