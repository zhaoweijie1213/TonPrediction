namespace TonPrediction.Infrastructure.Services;

using TonSdk.Client;
using TonSdk.Core;
using TonSdk.Core.Boc;

/// <summary>
/// TonClient 抽象，便于单元测试。
/// </summary>
public interface ITonClientWrapper
{
    /// <summary>
    /// 获取钱包公钥。
    /// </summary>
    Task<byte[]?> GetPublicKeyAsync(Address address);

    /// <summary>
    /// 获取钱包 seqno。
    /// </summary>
    Task<uint?> GetSeqnoAsync(Address address);

    /// <summary>
    /// 发送交易 BOC。
    /// </summary>
    Task<SendBocResult?> SendBocAsync(Cell boc);
}

/// <summary>
/// TonClient 包装实现。
/// </summary>
public sealed class TonClientWrapper(TonClient client) : ITonClientWrapper
{
    private readonly TonClient _client = client;
    public Task<byte[]?> GetPublicKeyAsync(Address address) => _client.Wallet.GetPublicKey(address);
    public Task<uint?> GetSeqnoAsync(Address address) => _client.Wallet.GetSeqno(address);
    public Task<SendBocResult?> SendBocAsync(Cell boc) => _client.SendBoc(boc);
}
