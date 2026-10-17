using System.Text.Json;
using LinePulse.Simulator.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace LinePulse.Simulator.Simulation;

public class MachineStateSimulatorService(
    RabbitMqConnectionManager connectionManager,
    IOptions<SimulationSettings> simulationOptions,
    ILogger<MachineStateSimulatorService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var machines = simulationOptions.Value.Machines;
        var delayScaleFactor = simulationOptions.Value.DelayScaleFactor;
        var faultInjectionRate = simulationOptions.Value.FaultInjectionRate;
        var faultInjectionFakeId = simulationOptions.Value.FaultInjectionFakeId;

        if (machines.Count == 0)
        {
            logger.LogWarning("No Machines configured under Simulation:Machines; MachineStateSimulatorService has nothing to do.");
            return;
        }

        var connection = await connectionManager.GetConnectionAsync(stoppingToken);
        await using (var setupChannel = await connection.CreateChannelAsync(cancellationToken: stoppingToken))
        {
            await RabbitMqEventRouter.DeclareTopologyAsync(setupChannel, stoppingToken);
        }

        logger.LogInformation("MachineStateSimulatorService connected; simulating {Count} machine(s).", machines.Count);

        try
        {
            await Task.WhenAll(machines.Select(profile => RunMachineLoopAsync(profile, connection, delayScaleFactor, faultInjectionRate, faultInjectionFakeId, stoppingToken)));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunMachineLoopAsync(MachineProfile profile, IConnection connection, double delayScaleFactor, double faultInjectionRate, Guid faultInjectionFakeId, CancellationToken stoppingToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        var durationBuckets = profile.GetWaitingDurationBuckets();

        while (!stoppingToken.IsCancellationRequested)
        {
            await DelayAsync(profile.RunningPeriodMilliseconds, delayScaleFactor, stoppingToken);

            var correlationId = Guid.NewGuid();
            var enteredWaitingAt = DateTimeOffset.UtcNow;

            var machineId = profile.MachineId;
            if (Random.Shared.NextDouble() < faultInjectionRate)
                machineId = faultInjectionFakeId;

            await PublishAsync(channel, RabbitMqEventRouter.MachineStateRoutingKey,
                new MachineStateChangedMessage(
                    RabbitMqEventRouter.MachineStateChangedEventType, profile.StationId, machineId,
                    SimulatedMachineState.Running, SimulatedMachineState.Waiting, correlationId, enteredWaitingAt),
                stoppingToken);

            var durationMilliseconds = WeightedRangePicker.Pick(durationBuckets);
            await DelayAsync(durationMilliseconds, delayScaleFactor, stoppingToken);

            var exitedWaitingAt = enteredWaitingAt + TimeSpan.FromMilliseconds(durationMilliseconds);

            await PublishAsync(channel, RabbitMqEventRouter.MachineStateRoutingKey,
                new MachineStateChangedMessage(
                    RabbitMqEventRouter.MachineStateChangedEventType, profile.StationId, machineId,
                    SimulatedMachineState.Waiting, SimulatedMachineState.Running, correlationId, exitedWaitingAt),
                stoppingToken);
        }
    }

    private async Task PublishAsync<TMessage>(IChannel channel, string routingKey, TMessage message, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
        var properties = new BasicProperties { Persistent = true };
        await channel.BasicPublishAsync(RabbitMqEventRouter.EventsExchange, routingKey, mandatory: false, properties, body, cancellationToken);
        logger.LogInformation("Published {RoutingKey}: {Message}", routingKey, JsonSerializer.Serialize(message, SerializerOptions));
    }

    private static async Task DelayAsync(long milliseconds, double delayScaleFactor, CancellationToken cancellationToken)
    {
        var scaledMilliseconds = Math.Max(0, milliseconds / delayScaleFactor);
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(scaledMilliseconds), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
    }
}
