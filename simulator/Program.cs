using Microsoft.Extensions.Options;
using LinePulse.Simulator.Messaging;
using LinePulse.Simulator.Simulation;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection(RabbitMqSettings.SectionName));
builder.Services.Configure<SimulationSettings>(builder.Configuration.GetSection(SimulationSettings.SectionName));
builder.Services.AddSingleton<RabbitMqConnectionManager>();

builder.Services.AddHostedService<CycleSimulatorService>();
builder.Services.AddHostedService<MachineStateSimulatorService>();

var app = builder.Build();

var simulationSettings = app.Services.GetRequiredService<IOptions<SimulationSettings>>().Value;
var validationErrors = simulationSettings.Validate();
if (validationErrors.Count > 0)
    throw new InvalidOperationException($"Invalid Simulation config in appsettings.json:\n{string.Join('\n', validationErrors)}");

var connectionManager = app.Services.GetRequiredService<RabbitMqConnectionManager>();
await connectionManager.GetConnectionAsync(CancellationToken.None);

app.Run();
