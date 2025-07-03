using PancakeSwap.Api.Hubs;
using QYQ.Base.Common.IOCExtensions;
using TonPrediction.Application.Database.Config;
using TonPrediction.Infrastructure.Database;
using TonPrediction.Infrastructure.Database.Migrations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddMultipleService("^TonPrediction");
builder.Services.AddSingleton<ApplicationDbContext>();
builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection("ConnectionStrings:Default"));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.MapHub<PredictionHub>("/predictionHub");

var dbContext = app.Services.GetRequiredService<ApplicationDbContext>();
InitMigration.Run(dbContext.Db);

app.Run();
