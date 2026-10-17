using LinePulse.Messaging;
using LinePulse.Redis;

namespace LinePulse.Processing;

public class CycleStartedHandler(RedisStateStore redisStateStore) : IEventHandler<CycleStartedMessage>
{
    public async Task HandleAsync(CycleStartedMessage message, CancellationToken cancellationToken)
    {
        _ = await redisStateStore.GetStationAsync(message.StationId)
            ?? throw new InvalidOperationException($"No cached station found for stationId '{message.StationId}'.");

        await redisStateStore.SetActiveCycleStartAsync(message.CorrelationId, message.StationId, message.OccurredAt);
    }
}