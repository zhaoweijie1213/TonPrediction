using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Api.Services.WalletListeners;

/// <summary>
/// 通过轮询 REST API 的钱包监听实现。
/// </summary>
public class RestWalletListener(ILogger<RestWalletListener> logger, IHttpClientFactory httpFactory) : IWalletListener
{
    private readonly HttpClient _http = httpFactory.CreateClient("TonApi");

    /// <inheritdoc />
    public async IAsyncEnumerable<TonTxDetail> ListenAsync(string walletAddress, ulong lastLt, [EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var url = $"/v2/blockchain/accounts/{walletAddress}/transactions?limit=5&to_lt={lastLt}";
            var resp = await _http.GetAsync(url, ct);

            var content = await resp.Content.ReadAsStringAsync(ct);

            logger.LogInformation("ListenAsync:{content}", content);

            var data = JsonConvert.DeserializeObject<AccountTxList>(content);

            if (data?.Transactions != null)
            {
                foreach (var tx in data.Transactions)
                {
                    if (tx.Lt > lastLt)
                    {
                        lastLt = tx.Lt;
                        yield return tx with { Lt = tx.Lt };
                    }
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
