using QYQ.Base.Common.IOCExtensions;
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
    /// <returns>领奖结果，失败返回 null。</returns>
    Task<ClaimOutput?> ClaimAsync(ClaimInput input);
}
