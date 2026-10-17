using LinePulse.Messaging;
using LinePulse.Processing;
using LinePulse.RabbitMq;
using LinePulse.Redis;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Serilog;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

// Serilog
builder.Services.AddSerilog(config => config
    .Enrich.FromLogContext()
    .WriteTo.Console());

// RabbitMq
builder.Services.Configure<Settings>(builder.Configuration.GetSection(Settings.SectionName));
builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<Settings>>().Value;

    return new ConnectionFactory
    {
        HostName = settings.Host,
        Port = settings.Port,
        VirtualHost = settings.VirtualHost,
        UserName = settings.Username,
        Password = settings.Password,
        ClientProvidedName = "LinePulse",
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true
    };
});

// Redis 
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection(RedisSettings.SectionName));
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<RedisSettings>>().Value;

    return ConnectionMultiplexer.Connect($"{settings.Host}:{settings.Port}");
});
builder.Services.AddSingleton<RedisStateStore>();

// LogsApi
builder.Services.Configure<LogsApiSettings>(builder.Configuration.GetSection(LogsApiSettings.SectionName));

// Event handler'lar
builder.Services.AddSingleton<IEventHandler<CycleStartedMessage>, CycleStartedHandler>();
builder.Services.AddSingleton<IEventHandler<CycleCompletedMessage>, CycleCompletedHandler>();
builder.Services.AddSingleton<IEventHandler<MachineStateChangedMessage>, MachineStateChangedHandler>();
builder.Services.AddHostedService<EventConsumerHostedService>();

var host = builder.Build();

// Başlangıç seedi appsettings'teki Stations/Machines tanımlarını Redis'e yazar
var redisStateStore = host.Services.GetRequiredService<RedisStateStore>();
var stations = builder.Configuration.GetSection("Stations").Get<List<StationDefinition>>() ?? [];
var machines = builder.Configuration.GetSection("Machines").Get<List<MachineDefinition>>() ?? [];

foreach (var station in stations)
{
    await redisStateStore.SetStationAsync(station.StationId, station.Name, station.ExpectedCycleMilliseconds, station.CycleToleranceMilliseconds);
}

foreach (var machine in machines)
{
    await redisStateStore.SetMachineAsync(machine.MachineId, machine.StationId, machine.Name, machine.MinDowntimeMilliseconds, machine.MicroStopLimitMilliseconds);
}

host.Run();

record StationDefinition(Guid StationId, string Name, long ExpectedCycleMilliseconds, long CycleToleranceMilliseconds);
record MachineDefinition(Guid MachineId, Guid StationId, string Name, long MinDowntimeMilliseconds, long MicroStopLimitMilliseconds);
