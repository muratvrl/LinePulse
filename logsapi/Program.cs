using LinePulse.LogsApi.Data;
using LinePulse.LogsApi.Repositories;
using LinePulse.LogsApi.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Services.AddSerilog(config => config
    .ReadFrom.Configuration(builder.Configuration));


builder.Services.AddControllers();

// Postgres EF
builder.Services.AddDbContext<LogsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Repository & Service
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<ILogService, LogService>();

var app = builder.Build();

// Serilog request logging
app.UseSerilogRequestLogging();

// Açılışta migration'ları otomatik uygula
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<LogsDbContext>().Database.MigrateAsync();
}

app.MapControllers();

await app.RunAsync();
