using System.Globalization;

namespace TonPrediction.Application.Extensions;

/// <summary>
/// decimal 扩展方法。
/// </summary>
public static class DecimalExtensions
{
    /// <summary>
    /// 将金额转换为字符串，最多保留 8 位小数并去除尾部 0。
    /// </summary>
    /// <param name="value">十进制数值。</param>
    /// <returns>格式化后的金额字符串。</returns>
    public static string ToAmountString(this decimal value)
        => decimal.Round(value, 8).ToString("0.########", CultureInfo.InvariantCulture);
}
