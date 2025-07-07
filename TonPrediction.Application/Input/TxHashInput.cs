namespace TonPrediction.Application.Input;

/// <summary>
/// 用户提交的交易哈希。
/// </summary>
public class TxHashInput
{
    /// <summary>
    /// 交易哈希字符串。
    /// </summary>
    public string TxHash { get; set; } = string.Empty;
}
