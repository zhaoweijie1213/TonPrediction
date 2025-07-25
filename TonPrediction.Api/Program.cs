using Microsoft.Extensions.DependencyInjection;
using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.Swagger.Extension;
using TonPrediction.Api.Hubs;
using TonPrediction.Api.Services;
using TonPrediction.Application.Config;
using TonPrediction.Application.Database.Config;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Application.Services.WalletListeners;
using TonPrediction.Infrastructure;
using TonPrediction.Infrastructure.Database;
using TonPrediction.Infrastructure.Database.Migrations;
using TonPrediction.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddQYQSerilog();
// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("TonApi", c =>
{
    var baseUrl = builder.Configuration["TonConfig:BaseUrl"] ?? "https://tonapi.io";
    c.BaseAddress = new Uri(baseUrl);
    var apiKey = builder.Configuration["TonConfig:ApiKey"];
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        c.DefaultRequestHeaders.Authorization = new("Bearer", apiKey);
    }
});
builder.Services.AddMultipleService("^TonPrediction");
builder.Services.AddSingleton<ApplicationDbContext>();
builder.Services.AddSingleton<IPriceService, BinancePriceService>();
builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<PredictionConfig>(builder.Configuration.GetSection("PredictionConfig"));
builder.Services.Configure<WalletConfig>(builder.Configuration.GetSection("WalletConfig"));
builder.Services.AddSingleton<SseWalletListener>();
builder.Services.AddSingleton<RestWalletListener>();
builder.Services.AddSingleton<WebSocketWalletListener>();
builder.Services.AddSingleton<IWalletListener>(sp =>
{
    var cfg = builder.Configuration.GetSection("WalletConfig").Get<WalletConfig>()!;
    return cfg.ListenerType switch
    {
        WalletListenerType.Rest => sp.GetRequiredService<RestWalletListener>(),
        WalletListenerType.WebSocket => sp.GetRequiredService<WebSocketWalletListener>(),
        _ => sp.GetRequiredService<SseWalletListener>()
    };
});
builder.Services.AddHostedService<RoundScheduler>();
builder.Services.AddHostedService<PriceMonitor>();
builder.Services.AddHostedService<TonEventListener>();

#region  CAP

builder.Services.AddCap(options =>
{
    var configuration = builder.Configuration;
    options.UseRabbitMQ(config =>
    {
        config.HostName = configuration["CAP:RabbitMQ:HostName"]!;
        config.Port = configuration.GetSection("CAP:RabbitMQ:Port").Get<int>();
        config.UserName = configuration["CAP:RabbitMQ:UserName"]!;
        config.Password = configuration["CAP:RabbitMQ:Password"]!;
        config.ExchangeName = configuration["CAP:RabbitMQ:ExchangeName"]!;
    });
    options.UseMySql(opt =>
    {
        opt.ConnectionString = configuration["ConnectionStrings:SysCap"]!;
        opt.TableNamePrefix = AppDomain.CurrentDomain.FriendlyName;
    });
});

#endregion

builder.AddQYQSwaggerAndApiVersioning(new NSwag.OpenApiInfo()
{
    Title = "TonPrediction API",
    Version = "v1",
    Description = "TonPrediction API for prediction game on TON blockchain."
}, null, false);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

builder.AddInfrastructure();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("AllowAllOrigins");

app.UseAuthorization();

app.UseQYQSwaggerUI("TonPrediction", false);

app.MapControllers();

app.MapHub<PredictionHub>("/predictionHub");

var dbContext = app.Services.GetRequiredService<ApplicationDbContext>();
InitMigration.Run(dbContext.Db);

app.Run();

/// <summary>
/// 
/// </summary>
public partial class Program { }
