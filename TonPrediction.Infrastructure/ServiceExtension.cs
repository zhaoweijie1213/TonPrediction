using EasyCaching.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Infrastructure.Services;

namespace TonPrediction.Infrastructure
{
    public static class ServiceExtension
    {
        /// <summary>
        /// 添加基础设施服务。
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
        {
            #region EasyCaching注册

            builder.Services.AddEasyCaching(options =>
            {
                options.UseRedis(config =>
                {
                    config.DBConfig = builder.Configuration.GetSection("Redis").Get<RedisDBOptions>();
                }, "DefaultRedis").WithMessagePack("DefaultRedis");
            });

            var redisSection = builder.Configuration.GetSection("Redis");
            var options = new ConfigurationOptions
            {
                Password = redisSection["Password"],
                AllowAdmin = redisSection.GetValue<bool>("AllowAdmin"),
                DefaultDatabase = redisSection.GetValue<int>("Database")
            };
            foreach (var ep in redisSection.GetSection("Endpoints").GetChildren())
            {
                options.EndPoints.Add($"{ep["Host"]}:{ep.GetValue<int>("Port")}");
            }
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options));
            builder.Services.AddSingleton<IDistributedLock, RedisDistributedLock>();
            builder.Services.AddSingleton<IWalletService, TonWalletService>();

            #endregion
            return builder;
        }
    }
}
