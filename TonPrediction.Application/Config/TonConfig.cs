using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TonPrediction.Application.Config
{
    /// <summary>
    /// TON  配置。
    /// </summary>
    public class TonConfig
    {
        /// <summary>
        /// 
        /// </summary>
        public const string BindingName = "TonConfig";

        /// <summary>
        /// BaseUrl
        /// </summary>
        public string BaseUrl { get; set; } = "https://tonapi.io";

        /// <summary>
        /// toncenter.io 的 API 端点。
        /// </summary>
        public string TonCenterEndPoint { get; set; } = string.Empty;

        /// <summary>
        /// api key for toncenter.io
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// websocket 连接地址。
        /// </summary>
        public string WebSocketUrl { get; set; } = string.Empty;
    }
}
