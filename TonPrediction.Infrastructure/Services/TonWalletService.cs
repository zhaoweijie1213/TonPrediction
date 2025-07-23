using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TonPrediction.Application.Config;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;
using TonSdk.Contracts.Wallet;
using TonSdk.Core;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;
using TonSdk.Core.Crypto;

namespace TonPrediction.Infrastructure.Services;

/// <summary>
/// 使用 TonSdk 通过 TonCenter 转账。
/// </summary>
public class TonWalletService(ILogger<TonWalletService> logger, ITonClientWrapper client, IOptions<WalletConfig> walletConfig) : IWalletService
{
    private readonly ITonClientWrapper _client = client;
    private readonly ILogger<TonWalletService> _logger = logger;
    private readonly string _walletVersion = walletConfig.Value.WalletVersion;
    private WalletV4? _wallet;


    /// <summary>
    /// 转账到指定地址。
    /// </summary>
    /// <param name="address"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public async Task<TransferResult> TransferAsync(string address, long amount, string? comment = null)
    {
        if (_walletVersion.Equals("w5", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("当前钱包版本为 W5，TonSdk.NET 暂不支持该版本");
            return new TransferResult(string.Empty, 0, DateTime.UtcNow, ClaimStatus.Failed);
        }
        // 拆分成词数组
        string[] words = walletConfig.Value.Mnemonic.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Mnemonic mnemonicObj = new Mnemonic(words);

        var key = mnemonicObj.Keys;

        try
        {
            if (_wallet is null)
            {
                _wallet = new WalletV4(new WalletV4Options { PublicKey = key.PublicKey, Workchain = 0, SubwalletId = 698983191 });
            }

            var seqno = await _client.GetSeqnoAsync(_wallet.Address) ?? 0u;
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
            ], seqno).Sign(mnemonicObj.Keys.PrivateKey, true);

            var result = await _client.SendBocAsync(message.Cell!);
            return new TransferResult(result?.Hash ?? string.Empty, 0, DateTime.UtcNow, ClaimStatus.Confirmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transfer failed");
            return new TransferResult(string.Empty, 0, DateTime.UtcNow, ClaimStatus.Failed);
        }
    }
}
