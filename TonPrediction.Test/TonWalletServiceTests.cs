using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using TonPrediction.Application.Config;
using TonPrediction.Application.Enums;
using TonPrediction.Infrastructure.Services;
using TonSdk.Client;
using TonSdk.Core;
using TonSdk.Core.Boc;
using TonSdk.Core.Crypto;
using Xunit;

namespace TonPrediction.Test;

/// <summary>
/// TonWalletService 单元测试。
/// </summary>
public class TonWalletServiceTests : IClassFixture<WebApplicationFactory<Program>>
{

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [Fact]
    public void GenerateWallet()
    {
        Mnemonic mnemonic = new();

        string words = string.Join(' ', mnemonic.Words);

        string pullicKey = Convert.ToHexString(mnemonic.Keys.PublicKey);

        string privateKey = Convert.ToHexString(mnemonic.Keys.PrivateKey);

        Assert.NotEmpty(words);

    }
}
