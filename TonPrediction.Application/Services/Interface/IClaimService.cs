using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Input;
using TonPrediction.Application.Output;

namespace TonPrediction.Application.Services.Interface;

/// <summary>
/// 领奖业务接口。
/// </summary>
public interface IClaimService : ITransientDependency
{
    /// <summary>
    /// 执行领奖操作。
    /// </summary>
    /// <param name="input">领奖参数。</param>
    /// <returns>业务结果。</returns>
    Task<ApiResult<ClaimOutput?>> ClaimAsync(ClaimInput input);
}
