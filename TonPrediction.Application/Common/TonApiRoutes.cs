namespace TonPrediction.Application.Common;

/// <summary>
/// Ton API 路由字符串集合。
/// </summary>
public static class TonApiRoutes
{
    /// <summary>
    /// 获取指定钱包交易列表。
    /// </summary>
    public const string AccountTransactions = "/v2/blockchain/accounts/{0}/transactions?limit={1}&to_lt={2}";

    /// <summary>
    /// SSE 监听钱包交易。
    /// </summary>
    public const string SseAccountTransactions = "/v2/sse/accounts/transactions?accounts={0}";

    /// <summary>
    /// 获取消息交易列表。
    /// </summary>
    public const string MessageTransactions = "/v2/blockchain/messages/{0}/transactions";

    /// <summary>
    /// 获取交易详情。
    /// </summary>
    public const string TransactionDetail = "/v2/blockchain/transactions/{0}";
}
