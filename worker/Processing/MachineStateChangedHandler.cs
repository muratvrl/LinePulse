using System.Net.Http.Json;
using LinePulse.Messaging;
using LinePulse.Redis;
using Microsoft.Extensions.Options;

namespace LinePulse.Processing;

public class MachineStateChangedHandler(RedisStateStore redisStateStore, IOptions<LogsApiSettings> logsApiOptions) : IEventHandler<MachineStateChangedMessage>
{
    private readonly HttpClient _httpClient = new() { BaseAddress = new Uri(logsApiOptions.Value.BaseUrl) };

    public async Task HandleAsync(MachineStateChangedMessage message, CancellationToken cancellationToken)
    {
        var correlationId = message.CorrelationId
            ?? throw new InvalidOperationException($"MachineStateChanged for machine '{message.MachineId}' is missing CorrelationId.");

        switch (message.CurrentState)
        {
            case SimulatedMachineState.Waiting:
                _ = await redisStateStore.GetMachineAsync(message.MachineId)
                    ?? throw new InvalidOperationException($"No cached machine found for machineId '{message.MachineId}'.");

                await redisStateStore.SetWaitingStartedAsync(correlationId, message.MachineId, message.OccurredAt);
                break;

            case SimulatedMachineState.Running:
            {
                var waitingRecord = await redisStateStore.GetWaitingStartedAsync(correlationId)
                    ?? throw new InvalidOperationException($"No waiting record found for correlationId '{correlationId}'.");

                var machine = await redisStateStore.GetMachineAsync(waitingRecord.MachineId)
                    ?? throw new InvalidOperationException($"No cached machine found for machineId '{waitingRecord.MachineId}'.");

                var station = await redisStateStore.GetStationAsync(machine.StationId)
                    ?? throw new InvalidOperationException($"No cached station found for stationId '{machine.StationId}'.");

                if (message.OccurredAt < waitingRecord.StartedAt)
                {
                    throw new InvalidOperationException($"Waiting ended ({message.OccurredAt:o}) before it started ({waitingRecord.StartedAt:o}).");
                }

                var durationMilliseconds = (long)Math.Round((message.OccurredAt - waitingRecord.StartedAt).TotalMilliseconds);

                string status;
                if (durationMilliseconds < machine.MinDowntimeMilliseconds)
                {
                    status = "Normal";
                }
                else if (durationMilliseconds < machine.MicroStopLimitMilliseconds)
                {
                    status = "MicroStop";
                }
                else
                {
                    status = "MacroStop";
                }

                var response = await _httpClient.PostAsJsonAsync("api/logs/machine-downtime", new
                {
                    correlationId,
                    machineId = waitingRecord.MachineId,
                    machineName = machine.Name,
                    stationId = machine.StationId,
                    stationName = station.Name,
                    downtimeStart = waitingRecord.StartedAt,
                    downtimeEnd = message.OccurredAt,
                    durationMs = durationMilliseconds,
                    status,
                    minDowntimeMs = machine.MinDowntimeMilliseconds,
                    microStopLimitMs = machine.MicroStopLimitMilliseconds
                }, cancellationToken);

                response.EnsureSuccessStatusCode();

                await redisStateStore.RemoveWaitingAsync(correlationId);

                break;
            }
        }
    }
}
