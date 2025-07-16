using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TonPrediction.Application.Config;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;
using TonSdk.Contracts.Wallet;
using TonSdk.Core;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;

namespace TonPrediction.Infrastructure.Services;

/// <summary>
/// 使用 TonSdk 通过 TonCenter 转账。
/// </summary>
public class TonWalletService(ILogger<TonWalletService> logger, ITonClientWrapper client, WalletConfig walletConfig) : IWalletService
{
    private readonly ITonClientWrapper _client = client;
    private readonly ILogger<TonWalletService> _logger = logger;
    private readonly Address _master = new(walletConfig.ENV_MASTER_WALLET_ADDRESS);
    private readonly byte[] _pk = Convert.FromHexString(walletConfig.ENV_MASTER_WALLET_PK);
    private PreprocessedV2? _wallet;
    private byte[]? _pubKey;

    /// <summary>
    /// 转账到指定地址。
    /// </summary>
    /// <param name="address"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public async Task<TransferResult> TransferAsync(string address, long amount, string? comment = null)
    {
        try
        {
            if (_wallet is null)
            {
                _pubKey = await _client.GetPublicKeyAsync(_master);
                _wallet = new PreprocessedV2(new PreprocessedV2Options { PublicKey = _pubKey!, Workchain = _master.GetWorkchain() });
            }

            var seqno = await _client.GetSeqnoAsync(_master) ?? 0u;
            var body = string.IsNullOrWhiteSpace(comment)
                ? new CellBuilder().Build()
                : new CellBuilder().StoreUInt(0, 32).StoreString(comment).Build();

            var message = _wallet.CreateTransferMessage(
            [
                new WalletTransfer
                {
                    Message = new InternalMessage(new()
                    {
                        Info = new IntMsgInfo(new()
                        {
                            Dest = new Address(address),
                            Value = new Coins((amount / 1_000_000_000m).ToString()),
                            Bounce = true
                        }),
                        Body = body
                    }),
                    Mode = 1
                }
            ], seqno).Sign(_pk, true);

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
