using System.Text.RegularExpressions;

namespace TonPrediction.Application.Common;

/// <summary>
/// 交易备注解析正则表达式集合。
/// </summary>
public static class CommentRegexCollection
{
    /// <summary>
    /// Bet 事件备注解析正则。
    /// 解析格式："Bet 1 bull"。
    /// </summary>
    public static readonly Regex Bet = new(
        @"^\s*(?<evt>\w+)\s+(?<rid>\d+)\s+(?<dir>bull|bear)\s*$",
        RegexOptions.IgnoreCase);
}
