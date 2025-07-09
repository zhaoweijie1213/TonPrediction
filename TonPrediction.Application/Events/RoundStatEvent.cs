using System;

namespace TonPrediction.Application.Events;

/// <summary>
/// 回合统计事件负载。
/// </summary>
/// <param name="Symbol">预测币种符号。</param>
/// <param name="RoundId">回合编号。</param>
public record RoundStatEvent(string Symbol, long RoundId);
