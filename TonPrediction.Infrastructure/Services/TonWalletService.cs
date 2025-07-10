using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TonSdk.Core;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;
using TonSdk.Contracts.Wallet;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Infrastructure.Services;

/// <summary>
/// 使用 TonSdk 通过 TonCenter 转账。
/// </summary>
public class TonWalletService(
    IConfiguration configuration,
    ITonClientWrapper client,
    ILogger<TonWalletService> logger) : IWalletService
{
    private readonly ITonClientWrapper _client = client;
    private readonly ILogger<TonWalletService> _logger = logger;
    private readonly Address _master = new(configuration["ENV_MASTER_WALLET_ADDRESS"] ?? string.Empty);
    private readonly byte[] _pk = Convert.FromHexString(configuration["ENV_MASTER_WALLET_PK"] ?? string.Empty);
    private PreprocessedV2? _wallet;
    private byte[]? _pubKey;

    /// <summary>
    /// 转账到指定地址。
    /// </summary>
    /// <param name="address"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public async Task<TransferResult> TransferAsync(string address, decimal amount)
    {
        try
        {
            if (_wallet is null)
            {
                _pubKey = await _client.GetPublicKeyAsync(_master);
                _wallet = new PreprocessedV2(new PreprocessedV2Options { PublicKey = _pubKey!, Workchain = _master.GetWorkchain() });
            }

            var seqno = await _client.GetSeqnoAsync(_master) ?? 0u;
            var message = _wallet.CreateTransferMessage(new[]
            {
                new WalletTransfer
                {
                    Message = new InternalMessage(new()
                    {
                        Info = new IntMsgInfo(new()
                        {
                            Dest = new Address(address),
                            Value = new Coins(amount.ToString()),
                            Bounce = true
                        }),
                        Body = new CellBuilder().Build()
                    }),
                    Mode = 1
                }
            }, seqno).Sign(_pk, true);

            var result = await _client.SendBocAsync(message.Cell!);
            return new TransferResult(result?.Hash ?? string.Empty, 0, DateTime.UtcNow, ClaimStatus.Confirmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transfer failed");
            return new TransferResult(string.Empty, 0, DateTime.UtcNow, ClaimStatus.Pending);
        }
    }
}
