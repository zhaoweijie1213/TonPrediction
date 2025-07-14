using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.Common.ApiResult;
using System.Threading;
using System.Threading.Tasks;

namespace TonPrediction.Application.Services.Interface;

/// <summary>
/// 下注上报业务接口。
/// </summary>
public interface IBetService : ITransientDependency
{
    /// <summary>
    /// 根据交易 BOC 上报下注并返回交易哈希。
    /// </summary>
    /// <param name="address">用户钱包地址。</param>
    /// <param name="boc">交易 BOC。</param>
    /// <returns>交易哈希。</returns>
    Task<ApiResult<string>> ReportAsync(string address, string boc);

    /// <summary>
    /// 验证指定回合是否可下注。
    /// </summary>
    /// <param name="roundId">回合编号。</param>
    /// <returns>验证结果。</returns>
    Task<ApiResult<bool>> VerifyAsync(long roundId);
}
