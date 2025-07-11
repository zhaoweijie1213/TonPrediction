using Newtonsoft.Json;
using System;

namespace TonPrediction.Application.Services;

/// <summary>
/// TonAPI 交易详情模型。
/// </summary>
public record TonTxDetail
{
    /// <summary>
    /// 交易哈希值。
    /// </summary>
    [JsonProperty("hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 账户逻辑时间。
    /// </summary>
    [JsonProperty("lt")]
    public ulong Lt { get; set; }

    /// <summary>
    /// 交易存在的账户信息。
    /// </summary>
    [JsonProperty("account")]
    public TxAccount Account { get; set; } = new();

    /// <summary>
    /// 交易是否执行成功。
    /// </summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Unix 时间戳。
    /// </summary>
    [JsonProperty("utime")]
    public ulong Utime { get; set; }

    /// <summary>
    /// 初始状态。
    /// </summary>
    [JsonProperty("orig_status")]
    public string Orig_Status { get; set; } = string.Empty;

    /// <summary>
    /// 最终状态。
    /// </summary>
    [JsonProperty("end_status")]
    public string End_Status { get; set; } = string.Empty;

    /// <summary>
    /// 所有费用累计。
    /// </summary>
    [JsonProperty("total_fees")]
    public ulong Total_Fees { get; set; }

    /// <summary>
    /// 交易结束时的账户余额。
    /// </summary>
    [JsonProperty("end_balance")]
    public ulong End_Balance { get; set; }

    /// <summary>
    /// 交易类型。
    /// </summary>
    [JsonProperty("transaction_type")]
    public string Transaction_Type { get; set; } = string.Empty;

    /// <summary>
    /// 状态新值的支付样本。
    /// </summary>
    [JsonProperty("state_update_old")]
    public string State_Update_Old { get; set; } = string.Empty;

    /// <summary>
    /// 状态更新后的值。
    /// </summary>
    [JsonProperty("state_update_new")]
    public string State_Update_New { get; set; } = string.Empty;

    /// <summary>
    /// 入库消息。
    /// </summary>
    [JsonProperty("in_msg")]
    public InMsg? In_Msg { get; set; }

    /// <summary>
    /// 出库消息列表。
    /// </summary>
    [JsonProperty("out_msgs")]
    public OutMsg[] Out_Msgs { get; set; } = Array.Empty<OutMsg>();

    /// <summary>
    /// 所属块信息。
    /// </summary>
    [JsonProperty("block")]
    public string Block { get; set; } = string.Empty;

    /// <summary>
    /// 上一条交易的哈希。
    /// </summary>
    [JsonProperty("prev_trans_hash")]
    public string Prev_Trans_Hash { get; set; } = string.Empty;

    /// <summary>
    /// 上一条交易的 lt 值。
    /// </summary>
    [JsonProperty("prev_trans_lt")]
    public ulong Prev_Trans_Lt { get; set; }

    /// <summary>
    /// 计算阶段详情。
    /// </summary>
    [JsonProperty("compute_phase")]
    public ComputePhase Compute_Phase { get; set; } = new();

    /// <summary>
    /// 存储阶段详情。
    /// </summary>
    [JsonProperty("storage_phase")]
    public StoragePhase Storage_Phase { get; set; } = new();

    /// <summary>
    /// 赋值阶段详情。
    /// </summary>
    [JsonProperty("credit_phase")]
    public CreditPhase Credit_Phase { get; set; } = new();

    /// <summary>
    /// 操作阶段详情。
    /// </summary>
    [JsonProperty("action_phase")]
    public ActionPhase Action_Phase { get; set; } = new();

    /// <summary>
    /// 是否中止。
    /// </summary>
    [JsonProperty("aborted")]
    public bool Aborted { get; set; }

    /// <summary>
    /// 是否消逝。
    /// </summary>
    [JsonProperty("destroyed")]
    public bool Destroyed { get; set; }

    /// <summary>
    /// 原始交易串。
    /// </summary>
    [JsonProperty("raw")]
    public string Raw { get; set; } = string.Empty;

    /// <summary>
    /// 返回的账户金额，为 In_Msg.Value 的方便展示。
    /// </summary>
    [JsonIgnore]
    public long Amount => In_Msg?.Value ?? 0;
}

/// <summary>
/// 交易账户信息。
/// </summary>
public class TxAccount
{
    /// <summary>
    /// 钱包地址。
    /// </summary>
    [JsonProperty("address")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 是否为骗子地址。
    /// </summary>
    [JsonProperty("is_scam")]
    public bool Is_Scam { get; set; }

    /// <summary>
    /// 是否钱包地址。
    /// </summary>
    [JsonProperty("is_wallet")]
    public bool Is_Wallet { get; set; }
}

/// <summary>
/// 入库消息模型。
/// </summary>
public class InMsg
{
    /// <summary>
    /// 消息类型。
    /// </summary>
    [JsonProperty("msg_type")]
    public string Msg_Type { get; set; } = string.Empty;

    /// <summary>
    /// 创建时的 lt。
    /// </summary>
    [JsonProperty("created_lt")]
    public ulong Created_Lt { get; set; }

    /// <summary>
    /// 是否禁用 Ihr。
    /// </summary>
    [JsonProperty("ihr_disabled")]
    public bool Ihr_Disabled { get; set; }

    /// <summary>
    /// 是否可点操作。
    /// </summary>
    [JsonProperty("bounce")]
    public bool Bounce { get; set; }

    /// <summary>
    /// 是否已被跳返。
    /// </summary>
    [JsonProperty("bounced")]
    public bool Bounced { get; set; }

    /// <summary>
    /// 金额值（nano TON）。
    /// </summary>
    [JsonProperty("value")]
    public long Value { get; set; }

    /// <summary>
    /// 转运费用。
    /// </summary>
    [JsonProperty("fwd_fee")]
    public ulong Fwd_Fee { get; set; }

    /// <summary>
    /// Ihr 费用。
    /// </summary>
    [JsonProperty("ihr_fee")]
    public ulong Ihr_Fee { get; set; }

    /// <summary>
    /// 目标地址。
    /// </summary>
    [JsonProperty("destination")]
    public AddressInfo Destination { get; set; } = new();

    /// <summary>
    /// 发送地址。
    /// </summary>
    [JsonProperty("source")]
    public AddressInfo Source { get; set; } = new();

    /// <summary>
    /// 导入费用。
    /// </summary>
    [JsonProperty("import_fee")]
    public ulong Import_Fee { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    [JsonProperty("created_at")]
    public ulong Created_At { get; set; }

    /// <summary>
    /// 操作码。
    /// </summary>
    [JsonProperty("op_code")]
    public string Op_Code { get; set; } = string.Empty;

    /// <summary>
    /// 消息哈希值。
    /// </summary>
    [JsonProperty("hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 原始运动文本。
    /// </summary>
    [JsonProperty("raw_body")]
    public string Raw_Body { get; set; } = string.Empty;

    /// <summary>
    /// 解码操作名称。
    /// </summary>
    [JsonProperty("decoded_op_name")]
    public string Decoded_Op_Name { get; set; } = string.Empty;

    /// <summary>
    /// 解码后的体内容。
    /// </summary>
    [JsonProperty("decoded_body")]
    public DecodedBody Decoded_Body { get; set; } = new();
}

/// <summary>
/// 账户地址信息。
/// </summary>
public class AddressInfo
{
    /// <summary>
    /// 地址值。
    /// </summary>
    [JsonProperty("address")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 是否是骗子地址。
    /// </summary>
    [JsonProperty("is_scam")]
    public bool Is_Scam { get; set; }

    /// <summary>
    /// 是否钱包地址。
    /// </summary>
    [JsonProperty("is_wallet")]
    public bool Is_Wallet { get; set; }
}

/// <summary>
/// 解码体内容。
/// </summary>
public class DecodedBody
{
    /// <summary>
    /// 解码文本。
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// 交易输出消息。
/// </summary>
public class OutMsg
{
    // 当前不需要详细字段，保留空的实体
}

/// <summary>
/// 计算阶段。
/// </summary>
public class ComputePhase
{
    /// <summary>
    /// 是否被跳过。
    /// </summary>
    [JsonProperty("skipped")]
    public bool Skipped { get; set; }

    /// <summary>
    /// VM 是否执行成功。
    /// </summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>
    /// 消耗的 gas 费用。
    /// </summary>
    [JsonProperty("gas_fees")]
    public ulong Gas_Fees { get; set; }

    /// <summary>
    /// 使用的 gas 量。
    /// </summary>
    [JsonProperty("gas_used")]
    public ulong Gas_Used { get; set; }

    /// <summary>
    /// VM 走步数。
    /// </summary>
    [JsonProperty("vm_steps")]
    public ulong Vm_Steps { get; set; }

    /// <summary>
    /// 退出码。
    /// </summary>
    [JsonProperty("exit_code")]
    public int Exit_Code { get; set; }

    /// <summary>
    /// 退出描述。
    /// </summary>
    [JsonProperty("exit_code_description")]
    public string Exit_Code_Description { get; set; } = string.Empty;
}

/// <summary>
/// 存储阶段信息。
/// </summary>
public class StoragePhase
{
    /// <summary>
    /// 收取的费用。
    /// </summary>
    [JsonProperty("fees_collected")]
    public ulong Fees_Collected { get; set; }

    /// <summary>
    /// 状态改变类型。
    /// </summary>
    [JsonProperty("status_change")]
    public string Status_Change { get; set; } = string.Empty;
}

/// <summary>
/// 赋值阶段信息。
/// </summary>
public class CreditPhase
{
    /// <summary>
    /// 收取费用。
    /// </summary>
    [JsonProperty("fees_collected")]
    public ulong Fees_Collected { get; set; }

    /// <summary>
    /// 赋上的金额。
    /// </summary>
    [JsonProperty("credit")]
    public decimal Credit { get; set; }
}

/// <summary>
/// 行动阶段信息。
/// </summary>
public class ActionPhase
{
    /// <summary>
    /// 是否成功。
    /// </summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>
    /// 结果码。
    /// </summary>
    [JsonProperty("result_code")]
    public int Result_Code { get; set; }

    /// <summary>
    /// 总作用数量。
    /// </summary>
    [JsonProperty("total_actions")]
    public int Total_Actions { get; set; }

    /// <summary>
    /// 被跳过的作用数。
    /// </summary>
    [JsonProperty("skipped_actions")]
    public int Skipped_Actions { get; set; }

    /// <summary>
    /// 转运费用。
    /// </summary>
    [JsonProperty("fwd_fees")]
    public ulong Fwd_Fees { get; set; }

    /// <summary>
    /// 总费用。
    /// </summary>
    [JsonProperty("total_fees")]
    public ulong Total_Fees { get; set; }
}
