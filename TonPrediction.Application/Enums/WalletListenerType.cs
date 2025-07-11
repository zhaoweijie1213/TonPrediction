namespace TonPrediction.Application.Enums;

/// <summary>
/// 钱包监听类型。
/// </summary>
public enum WalletListenerType
{
    /// <summary>
    /// 使用 Server-Sent Events。
    /// </summary>
    Sse,

    /// <summary>
    /// 通过定期轮询 REST API。
    /// </summary>
    Rest,

    /// <summary>
    /// 通过 WebSocket 订阅。
    /// </summary>
    WebSocket
}
