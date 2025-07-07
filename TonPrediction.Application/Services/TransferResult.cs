namespace TonPrediction.Application.Services;

using TonPrediction.Application.Enums;

/// <summary>
/// 表示转账交易结果。
/// </summary>
/// <param name="TxHash">交易哈希。</param>
/// <param name="Lt">账户逻辑时间。</param>
/// <param name="Timestamp">交易发生时间。</param>
/// <param name="Status">交易状态。</param>
public record TransferResult(
    string TxHash,
    ulong Lt,
    DateTime Timestamp,
    ClaimStatus Status);
