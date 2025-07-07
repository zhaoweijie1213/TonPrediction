using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TonPrediction.Application.Cache
{
    /// <summary>
    /// 缓存key集合
    /// </summary>
    public static class CacheKeyCollection
    {
        /// <summary>
        /// 生成回合缓存key。
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string GetTonPriceKey(string address)
        {
            return $"{AppDomain.CurrentDomain.FriendlyName}tonprice:{address}";
        }

        /// <summary>
        /// 分布式锁：RoundScheduler。
        /// </summary>
        public static string RoundSchedulerLockKey =>
            $"{AppDomain.CurrentDomain.FriendlyName}lock:round_scheduler";

        /// <summary>
        /// 分布式锁：PriceMonitor。
        /// </summary>
        public static string PriceMonitorLockKey =>
            $"{AppDomain.CurrentDomain.FriendlyName}lock:price_monitor";

        /// <summary>
        /// 分布式锁：TonEventListener。
        /// </summary>
        public static string TonEventListenerLockKey =>
            $"{AppDomain.CurrentDomain.FriendlyName}lock:ton_event_listener";
    }
}
