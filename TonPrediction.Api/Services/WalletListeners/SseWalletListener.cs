using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Api.Services.WalletListeners;

/// <summary>
/// 基于 SSE 的钱包监听实现。
/// </summary>
public class SseWalletListener(IHttpClientFactory httpFactory, ILogger<SseWalletListener> logger) : IWalletListener
{
    private readonly HttpClient _http = httpFactory.CreateClient("TonApi");
    private readonly ILogger<SseWalletListener> _logger = logger;
    private const string SseUrlTemplate = "/v2/sse/accounts/transactions?accounts={0}";

    /// <inheritdoc />
    public async IAsyncEnumerable<TonTxDetail> ListenAsync(string walletAddress, ulong lastLt, [EnumeratorCancellation] CancellationToken ct)
    {
        if (lastLt > 0)
        {
            var history = await FetchMissedListAsync(walletAddress, lastLt, ct);
            foreach (var tx in history)
            {
                yield return tx;
                lastLt = tx.Lt;
            }
        }

        var backoff = TimeSpan.FromSeconds(3);
        while (!ct.IsCancellationRequested)
        {
            var items = new List<TonTxDetail>();
            try
            {
                await using var stream = await _http.GetStreamAsync(string.Format(SseUrlTemplate, walletAddress), ct);
                using var reader = new StreamReader(stream);
                string? eventName = null;
                while (!reader.EndOfStream && !ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(ct);
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.StartsWith("event:"))
                    {
                        eventName = line["event:".Length..].Trim();
                        continue;
                    }
                    if (line.StartsWith("data:") && eventName == "message")
                    {
                        var json = line["data:".Length..].Trim();
                        var head = JsonConvert.DeserializeObject<SseTxHead>(json)!;
                        var detail = await _http.GetFromJsonAsync<TonTxDetail>($"/v2/blockchain/transactions/{head.Tx_Hash}", ct);
                        if (detail != null)
                        {
                            items.Add(detail with { Hash = head.Tx_Hash, Lt = head.Lt });
                            lastLt = head.Lt;
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                yield break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SSE error");
                items.AddRange(await FetchMissedListAsync(walletAddress, lastLt, ct));
                await Task.Delay(backoff, ct);
                backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, 30));
            }

            foreach (var tx in items)
            {
                yield return tx;
            }
        }
    }

    private async Task<List<TonTxDetail>> FetchMissedListAsync(string walletAddress, ulong lastLt, CancellationToken ct)
    {
        var list = new List<TonTxDetail>();
        var url = $"/v2/blockchain/accounts/{walletAddress}/transactions?limit=20&to_lt={lastLt}";
        try
        {
            var resp = await _http.GetFromJsonAsync<AccountTxList>(url, ct);
            if (resp?.Transactions != null)
            {
                foreach (var tx in resp.Transactions)
                {
                    list.Add(tx with { Lt = tx.Lt });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fetch history error");
        }

        return list;
    }
}
