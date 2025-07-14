namespace TonPrediction.Application.Input;

/// <summary>
/// 下注上报请求参数。
/// </summary>
public class BetReportInput
{
    /// <summary>
    /// 用户钱包地址。
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 交易 BOC 字符串。
    /// </summary>
    public string Boc { get; set; } = string.Empty;
}
