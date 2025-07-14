namespace TonPrediction.Application.Services.WalletListeners;

/// <summary>
/// TonAPI SSE "message" 事件载荷。
/// </summary>
/// <param name="Account_Id">账户标识。</param>
/// <param name="Lt">账户逻辑时间。</param>
/// <param name="Tx_Hash">交易哈希。</param>
public record SseTxHead(string Account_Id, ulong Lt, string Tx_Hash);

/// <summary>
/// TonAPI 账户交易列表响应。
/// </summary>
/// <param name="Transactions">交易数组。</param>
public record AccountTxList(TonPrediction.Application.Services.TonTxDetail[] Transactions);
