using EasyCaching.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TonPrediction.Application.Config;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Infrastructure.Services;
using TonSdk.Client;

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

            #endregion
            //var redisSection = builder.Configuration.GetSection("Redis");
            //var options = new ConfigurationOptions
            //{
            //    Password = redisSection["Password"],
            //    AllowAdmin = redisSection.GetValue<bool>("AllowAdmin"),
            //    DefaultDatabase = redisSection.GetValue<int>("Database")
            //};
            //foreach (var ep in redisSection.GetSection("Endpoints").GetChildren())
            //{
            //    options.EndPoints.Add($"{ep["Host"]}:{ep.GetValue<int>("Port")}");
            //}
            //builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options));
            builder.Services.AddSingleton<IDistributedLock, RedisDistributedLock>();

            #region TON SDK Client

            var tonConfig = builder.Configuration.GetSection(TonConfig.BindingName);
            builder.Services.Configure<TonConfig>(tonConfig);
            var tonParams = new HttpParameters
            {
                Endpoint = tonConfig.Get<TonConfig>()?.TonCenterEndPoint ?? "https://toncenter.com/api/v2/jsonRPC",
                ApiKey = tonConfig.Get<TonConfig>()?.ApiKey ?? string.Empty
            };
            var tonClient = new TonClient(TonClientType.HTTP_TONCENTERAPIV2, tonParams);
            builder.Services.AddSingleton<ITonClientWrapper>(new TonClientWrapper(tonClient));
            builder.Services.AddSingleton<IWalletService, TonWalletService>();

            #endregion

            return builder;
        }
    }
}
