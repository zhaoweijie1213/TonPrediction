using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Infrastructure.Services;

/// <summary>
/// 基于 TonSdk 的转账实现，占位示例。
/// </summary>
public class TonWalletService(
    IConfiguration configuration,
    IHttpClientFactory httpFactory,
    ILogger<TonWalletService> logger) : IWalletService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly HttpClient _http = httpFactory.CreateClient("TonApi");
    private readonly ILogger<TonWalletService> _logger = logger;
    private readonly string _wallet = configuration["ENV_MASTER_WALLET_ADDRESS"] ?? string.Empty;

    /// <inheritdoc />
    public async Task<TransferResult> TransferAsync(
        string address,
        decimal amount)
    {
        try
        {
            var body = new
            {
                to = address,
                amount = ((ulong)(amount * 1_000_000_000m)).ToString(),
                bounce = false
            };
            var resp = await _http.PostAsJsonAsync(
                $"/v2/blockchain/accounts/{_wallet}/transfer",
                body);
            resp.EnsureSuccessStatusCode();
            var data = await resp.Content.ReadFromJsonAsync<Response>();
            return new TransferResult(
                data?.Hash ?? string.Empty,
                data?.Lt ?? 0,
                DateTime.UtcNow,
                ClaimStatus.Confirmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transfer failed");
            return new TransferResult(string.Empty, 0, DateTime.UtcNow, ClaimStatus.Pending);
        }
    }

    private sealed class Response
    {
        public string Hash { get; set; } = string.Empty;
        public ulong Lt { get; set; }
    }
}
