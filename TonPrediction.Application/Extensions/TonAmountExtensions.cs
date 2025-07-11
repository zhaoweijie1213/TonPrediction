using System;

namespace TonPrediction.Application.Extensions;

/// <summary>
/// nano TON 与普通 TON 之间的转换工具。
/// </summary>
public static class TonAmountExtensions
{
    private const decimal NanoFactor = 1_000_000_000m;

    /// <summary>
    /// 将 TON 金额转换为 nano TON。
    /// </summary>
    /// <param name="ton">普通 TON 金额。</param>
    /// <returns>对应的 nano TON 整数。</returns>
    public static long ToNanoTon(this decimal ton)
        => (long)Math.Round(ton * NanoFactor);

    /// <summary>
    /// 将 nano TON 转换为普通 TON 字符串。
    /// </summary>
    /// <param name="nanoTon">nano TON 金额。</param>
    /// <returns>格式化后的 TON 字符串。</returns>
    public static string ToAmountString(this long nanoTon)
        => ((decimal)nanoTon / NanoFactor).ToAmountString();
}
