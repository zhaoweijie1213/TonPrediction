using System;

namespace TonPrediction.Application.Events;

/// <summary>
/// 平局回合退款事件负载。
/// </summary>
/// <param name="Symbol">预测币种符号。</param>
/// <param name="RoundId">回合编号。</param>
public record RoundRefundEvent(string Symbol, long RoundId);
