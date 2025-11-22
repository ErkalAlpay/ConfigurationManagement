using Configuration.Core.Service;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = builder.Configuration.GetConnectionString("Redis");
    if (string.IsNullOrEmpty(config))
        throw new InvalidOperationException("Redis connection string is not configured.");
    return ConnectionMultiplexer.Connect(config);
});

builder.Services.AddScoped<ICacheService, RedisCacheService>();

// ConfigurationReader artık connection string ile çalışıyor
builder.Services.AddSingleton<ConfigurationReader>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return new ConfigurationReader(
        applicationName: "SERVICE-A",
        connectionString: connectionString,
        refreshTimerIntervalInMs: 10000
    );
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

app.Run();
