using System.Net.Http.Json;
using LinePulse.Messaging;
using LinePulse.Redis;
using Microsoft.Extensions.Options;

namespace LinePulse.Processing;

public enum CycleStatus
{
    Normal,
    Overrun,
    Underrun
}

public class CycleCompletedHandler(RedisStateStore redisStateStore, IOptions<LogsApiSettings> logsApiOptions) : IEventHandler<CycleCompletedMessage>
{
    private readonly HttpClient _httpClient = new() { BaseAddress = new Uri(logsApiOptions.Value.BaseUrl) };

    public async Task HandleAsync(CycleCompletedMessage message, CancellationToken cancellationToken)
    {
        var activeCycle = await redisStateStore.GetActiveCycleAsync(message.CorrelationId)
            ?? throw new InvalidOperationException($"No active cycle found for correlationId '{message.CorrelationId}'.");

        var station = await redisStateStore.GetStationAsync(activeCycle.StationId)
            ?? throw new InvalidOperationException($"No cached station found for stationId '{activeCycle.StationId}'.");

        if (message.OccurredAt < activeCycle.StartedAt)
        {
            throw new InvalidOperationException($"Cycle ended ({message.OccurredAt:o}) before it started ({activeCycle.StartedAt:o}).");
        }

        var durationMilliseconds = (long)Math.Round((message.OccurredAt - activeCycle.StartedAt).TotalMilliseconds);
        var allowedMaximumMilliseconds = station.ExpectedCycleMilliseconds + station.CycleToleranceMilliseconds;
        var allowedMinimumMilliseconds = station.ExpectedCycleMilliseconds - station.CycleToleranceMilliseconds;

        CycleStatus status;
        if (durationMilliseconds > allowedMaximumMilliseconds)
        {
            status = CycleStatus.Overrun;
        }
        else if (durationMilliseconds < allowedMinimumMilliseconds)
        {
            status = CycleStatus.Underrun;
        }
        else
        {
            status = CycleStatus.Normal;
        }

        var response = await _httpClient.PostAsJsonAsync("api/logs/cycle", new
        {
            correlationId = message.CorrelationId,
            stationId = activeCycle.StationId,
            stationName = station.Name,
            cycleStart = activeCycle.StartedAt,
            cycleEnd = message.OccurredAt,
            durationMs = durationMilliseconds,
            status = status.ToString(),
            expectedCycleMs = station.ExpectedCycleMilliseconds,
            toleranceMs = station.CycleToleranceMilliseconds
        }, cancellationToken);

        response.EnsureSuccessStatusCode();

        await redisStateStore.RemoveActiveCycleAsync(message.CorrelationId);
    }
}
