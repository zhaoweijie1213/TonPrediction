using QYQ.Base.Common.IOCExtensions;
using TonPrediction.Application.Services;

namespace TonPrediction.Application.Services.Interface;

/// <summary>
/// 钱包转账服务接口。
/// </summary>
public interface IWalletService : ITransientDependency
{
    /// <summary>
    /// 从主钱包向指定地址转账。
    /// </summary>
    /// <param name="address">目标地址。</param>
    /// <param name="amount">转账金额。</param>
    /// <param name="comment">转账备注，可空。</param>
    /// <returns>转账结果。</returns>
    Task<TransferResult> TransferAsync(string address, long amount, string? comment = null);
}
