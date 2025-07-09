using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.Swagger.Extension;
using TonPrediction.Api.Hubs;
using TonPrediction.Api.Services;
using TonPrediction.Application.Database.Config;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Infrastructure;
using TonPrediction.Infrastructure.Database;
using TonPrediction.Infrastructure.Database.Migrations;
using TonPrediction.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddQYQSerilog();
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("TonApi", c =>
{
    var baseUrl = builder.Configuration["TonApi:BaseUrl"] ?? "https://tonapi.io";
    c.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddMultipleService("^TonPrediction");
builder.Services.AddSingleton<ApplicationDbContext>();
builder.Services.AddSingleton<IPriceService, BinancePriceService>();
builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.AddHostedService<RoundScheduler>();
builder.Services.AddHostedService<PriceMonitor>();
builder.Services.AddHostedService<TonEventListener>();
builder.Services.AddTransient<StatEventHandler>();

builder.Services.AddCap(options =>
{
    options.UseMySql(builder.Configuration.GetConnectionString("Default"));
    options.UseRabbitMQ(cfg =>
    {
        cfg.HostName = builder.Configuration["ENV_RABBITMQ_HOST"] ?? "localhost";
        cfg.UserName = builder.Configuration["ENV_RABBITMQ_USER"] ?? "guest";
        cfg.Password = builder.Configuration["ENV_RABBITMQ_PASSWORD"] ?? "guest";
    });
    options.UseDashboard();
});

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
