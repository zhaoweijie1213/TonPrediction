using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using TonSdk.Core;
using TonSdk.Core.Boc;
using TonSdk.Client;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Config;
using TonPrediction.Infrastructure.Services;
using Xunit;

namespace TonPrediction.Test;

/// <summary>
/// TonWalletService 单元测试。
/// </summary>
public class TonWalletServiceTests
{
    private sealed class FakeClient : ITonClientWrapper
    {
        public bool SendCalled { get; private set; }
        public Task<byte[]?> GetPublicKeyAsync(Address address) => Task.FromResult<byte[]?>(new byte[32]);
        public Task<uint?> GetSeqnoAsync(Address address) => Task.FromResult<uint?>(1);
        public Task<SendBocResult?> SendBocAsync(Cell boc)
        {
            SendCalled = true;
            return Task.FromResult<SendBocResult?>(new SendBocResult { Hash = "h" });
        }
    }

    [Fact]
    public async Task TransferAsync_SendsBoc()
    {
        var client = new FakeClient();
        var walletConfig = new WalletConfig
        {
            ENV_MASTER_WALLET_ADDRESS = "EQBlHnYC0Uk13_WBK4PN-qjB2TiiXixYDTe7EjX17-IV-0eF",
            ENV_MASTER_WALLET_PK = "0000000000000000000000000000000000000000000000000000000000000000"
        };
        var service = new TonWalletService(NullLogger<TonWalletService>.Instance, client, walletConfig);

        var result = await service.TransferAsync("EQBlHnYC0Uk13_WBK4PN-qjB2TiiXixYDTe7EjX17-IV-0eF", 1_000_000_000, null);

        Assert.True(client.SendCalled);
        Assert.Equal("h", result.TxHash);
        Assert.Equal(ClaimStatus.Confirmed, result.Status);
    }
}
