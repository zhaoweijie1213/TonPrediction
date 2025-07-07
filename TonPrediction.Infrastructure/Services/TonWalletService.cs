using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Infrastructure.Services;

/// <summary>
/// 基于 TonSdk 的转账实现，占位示例。
/// </summary>
public class TonWalletService(
    IConfiguration configuration,
    ILogger<TonWalletService> logger) : IWalletService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<TonWalletService> _logger = logger;

    /// <inheritdoc />
    public async Task<TransferResult> TransferAsync(
        string address,
        decimal amount,
        CancellationToken ct = default)
    {
        // TODO: 使用 TonSdk 发送真实交易，目前仅返回模拟结果。
        await Task.Delay(100, ct);
        var hash = Guid.NewGuid().ToString("N");
        return new TransferResult(hash, 0, DateTime.UtcNow, ClaimStatus.Confirmed);
    }
}
