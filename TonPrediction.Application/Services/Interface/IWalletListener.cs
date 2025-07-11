using TonPrediction.Application.Services;

namespace TonPrediction.Application.Services.Interface;

/// <summary>
/// 钱包事件监听接口。
/// </summary>
public interface IWalletListener
{
    /// <summary>
    /// 开始监听指定钱包地址的新交易。
    /// </summary>
    /// <param name="walletAddress">钱包地址。</param>
    /// <param name="lastLt">上次处理的逻辑时间。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>交易异步枚举。</returns>
    IAsyncEnumerable<TonTxDetail> ListenAsync(string walletAddress, ulong lastLt, CancellationToken ct);
}
