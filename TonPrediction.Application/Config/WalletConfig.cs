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
        public string ENV_MASTER_WALLET_ADDRESS { get; set; } = string.Empty;

        /// <summary>
        /// 主钱包私钥
        /// </summary>
        public string ENV_MASTER_WALLET_PK { get; set; } = string.Empty;

        /// <summary>
        /// 钱包监听方式，默认为 Sse。
        /// </summary>
        public WalletListenerType ListenerType { get; set; } = WalletListenerType.Sse;
    }
}
