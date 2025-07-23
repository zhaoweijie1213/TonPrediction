using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TonPrediction.Application.Enums;

namespace TonPrediction.Application.Config
{
    /// <summary>
    /// 钱包配置
    /// </summary>
    public class WalletConfig
    {
        /// <summary>
        /// 主钱包地址。
        /// </summary>
        public string MasterWalletAddress { get; set; } = string.Empty;

        /// <summary>
        /// 助记词
        /// </summary>
        public string Mnemonic { get; set; } = string.Empty;

        /// <summary>
        /// 钱包子钱包 ID
        /// </summary>
        public uint SubwalletId { get; set; }

        /// <summary>
        /// 主钱包私钥
        /// </summary>
        public string MasterWalletPk { get; set; } = string.Empty;

        /// <summary>
        /// 主钱包公钥
        /// </summary>
        public string MasterWalletPublicKey { get; set; } = string.Empty;

        /// <summary>
        /// 钱包监听方式，默认为 Sse。
        /// </summary>
        public WalletListenerType ListenerType { get; set; } = WalletListenerType.Sse;

        /// <summary>
        /// 钱包合约版本，如 "v4"、"w5" 等。
        /// </summary>
        public string WalletVersion { get; set; } = "wallet_v4r2";
    }
}
