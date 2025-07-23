using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Json;
using TonPrediction.Application.Common;
using TonPrediction.Application.Services.WalletListeners;
using TonPrediction.Application.Config;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Extensions;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;
using TonSdk.Client;
using TonSdk.Contracts.Wallet;
using TonSdk.Core;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;
using TonSdk.Core.Crypto;

namespace TonPrediction.Infrastructure.Services;

/// <summary>
/// 使用 TonSdk 通过 TonCenter 转账。
/// </summary>
public class TonWalletService(ILogger<TonWalletService> logger, ITonClient client, IOptions<WalletConfig> walletConfig, IHttpClientFactory httpFactory) : IWalletService
{
    private readonly ITonClient _client = client;
    private readonly ILogger<TonWalletService> _logger = logger;
    private readonly WalletConfig _config = walletConfig.Value;
    private readonly string _walletVersion = walletConfig.Value.WalletVersion;
    private readonly HttpClient _http = httpFactory.CreateClient("TonApi");

    /// <summary>
    /// 钱包地址，使用 MasterWalletAddress 初始化。
    /// </summary>
    private readonly Address _address = new Address(walletConfig.Value.MasterWalletAddress, new AddressStringifyOptions(false, true, true, 0));

    /// <summary>
    /// 公钥
    /// </summary>
    private readonly byte[] _publicKey = Convert.FromHexString(walletConfig.Value.MasterWalletPublicKey);

    /// <summary>
    /// 私钥
    /// </summary>
    private readonly byte[] _privateKey = Convert.FromHexString(walletConfig.Value.MasterWalletPrivateKey);
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
        //// 拆分成词数组
        //string[] words = walletConfig.Value.Mnemonic.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //Mnemonic mnemonicObj = new Mnemonic(words);

        try
        {
            if (_wallet is null)
            {
                //var publicKey = await _client.Wallet.GetPublicKey(_address);

                //var subwalletId = await _client.Wallet.GetSubwalletId(_address);

                //if (publicKey == null || publicKey != _publicKey)
                //{
                //    _logger.LogWarning("钱包地址 {Address} 的公钥不匹配，重新创建钱包实例", _address);
                //}
                _wallet = new WalletV4(new WalletV4Options { PublicKey = Convert.FromHexString(_config.MasterWalletPublicKey) }, 2);
            }

            var coins = await _client.GetBalance(_address);

            if (amount > coins.ToBigInt())
            {
                _logger.LogWarning("当前钱包余额不足");
                return new TransferResult(string.Empty, 0, DateTime.UtcNow, ClaimStatus.Failed);
            }

            uint seqno = await _client.Wallet.GetSeqno(_address) ?? 0;
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
                            Dest = new Address(address, new AddressStringifyOptions(false, true, true, 0)),
                            Value = new Coins(amount.ToTon()),
                            Src = _address,
                        }),
                        Body = body
                    }),
                    Mode = 1
                }
            ], seqno);

            message.Sign(_privateKey);

            var msgHash = Convert.ToHexString(message.Cell.Hash.ToBytes()).ToLowerInvariant();

            var result = await _client.SendBoc(message.Cell);

            var (txHash, lt, utime) = await WaitTxAsync(_config.MasterWalletAddress, msgHash);

            if (string.IsNullOrEmpty(txHash))
            {
                return new TransferResult(string.Empty, 0, DateTime.UtcNow, ClaimStatus.Failed);
            }

            return new TransferResult(txHash, lt, DateTimeOffset.FromUnixTimeSeconds((long)utime).UtcDateTime, ClaimStatus.Confirmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transfer failed");
            return new TransferResult(string.Empty, 0, DateTime.UtcNow, ClaimStatus.Failed);
        }
    }

    private async Task<(string txHash, ulong lt, ulong utime)> WaitTxAsync(string address, string msgHash)
    {
        ulong lastLt = 0;
        while (true)
        {
            var url = string.Format(TonApiRoutes.AccountTransactions, address, 20, lastLt);
            var resp = await _http.GetFromJsonAsync<AccountTxList>(url);
            if (resp?.Transactions != null)
            {
                foreach (var tx in resp.Transactions)
                {
                    if (string.Equals(tx.In_Msg?.Hash, msgHash, StringComparison.OrdinalIgnoreCase))
                    {
                        return (tx.Hash, tx.Lt, tx.Utime);
                    }
                }
                lastLt = resp.Transactions[0].Lt;
            }
            await Task.Delay(1000);
        }
    }
}
