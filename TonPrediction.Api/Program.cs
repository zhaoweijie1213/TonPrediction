using PancakeSwap.Api.Hubs;
using QYQ.Base.Common.IOCExtensions;
using TonPrediction.Application.Database.Config;
using TonPrediction.Infrastructure.Database;
using TonPrediction.Infrastructure.Database.Migrations;
using TonPrediction.Api.Services;
using TonPrediction.Infrastructure.Services;
using TonPrediction.Application.Services.Interface;

var builder = WebApplication.CreateBuilder(args);
builder.AddQYQSerilog();
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddMultipleService("^TonPrediction");
builder.Services.AddSingleton<ApplicationDbContext>();
builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.AddHostedService<RoundScheduler>();
builder.Services.AddHostedService<PriceMonitor>();
builder.Services.AddHostedService<TonEventListener>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.MapHub<PredictionHub>("/predictionHub");

var dbContext = app.Services.GetRequiredService<ApplicationDbContext>();
InitMigration.Run(dbContext.Db);

app.Run();
