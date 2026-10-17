using System.Globalization;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LinePulse.Redis;

public class RedisStateStore(IConnectionMultiplexer connectionMultiplexer, IOptions<RedisSettings> options)
{
    private IDatabase Database => connectionMultiplexer.GetDatabase();
    private TimeSpan CorrelationTtl => TimeSpan.FromHours(options.Value.CorrelationTtlHours);

    // active_cycle:{correlationId} — bir döngünün başlangıç kaydı, tamamlandığında correlationId ile bulunur
    private static string ActiveCycleKey(Guid correlationId) => $"active_cycle:{correlationId}";

    public async Task SetActiveCycleStartAsync(Guid correlationId, Guid stationId, DateTimeOffset startedAt)
    {
        var key = ActiveCycleKey(correlationId);

        await Database.HashSetAsync(key,
        [
            new HashEntry("station_id", stationId.ToString()),
            new HashEntry("cycle_start", startedAt.UtcDateTime.ToString("O"))
        ]);

        await Database.KeyExpireAsync(key, CorrelationTtl);
    }

    public async Task<(Guid StationId, DateTimeOffset StartedAt)?> GetActiveCycleAsync(Guid correlationId)
    {
        var values = await Database.HashGetAsync(ActiveCycleKey(correlationId), ["station_id", "cycle_start"]);

        if (values[0].IsNullOrEmpty || values[1].IsNullOrEmpty)
        {
            return null;
        }

        return (Guid.Parse(values[0].ToString()), DateTimeOffset.Parse(values[1].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }

    public Task RemoveActiveCycleAsync(Guid correlationId) =>
        Database.KeyDeleteAsync(ActiveCycleKey(correlationId));

    // machine_waiting:{correlationId} — bir bekleme aralığının başlangıç kaydı, bittiğinde correlationId ile bulunur
    private static string MachineWaitingKey(Guid correlationId) => $"machine_waiting:{correlationId}";

    public async Task SetWaitingStartedAsync(Guid correlationId, Guid machineId, DateTimeOffset startedAt)
    {
        var key = MachineWaitingKey(correlationId);

        await Database.HashSetAsync(key,
        [
            new HashEntry("machine_id", machineId.ToString()),
            new HashEntry("waiting_start", startedAt.UtcDateTime.ToString("O"))
        ]);

        await Database.KeyExpireAsync(key, CorrelationTtl);
    }

    public async Task<(Guid MachineId, DateTimeOffset StartedAt)?> GetWaitingStartedAsync(Guid correlationId)
    {
        var values = await Database.HashGetAsync(MachineWaitingKey(correlationId), ["machine_id", "waiting_start"]);

        if (values[0].IsNullOrEmpty || values[1].IsNullOrEmpty)
        {
            return null;
        }

        return (Guid.Parse(values[0].ToString()), DateTimeOffset.Parse(values[1].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }

    public Task RemoveWaitingAsync(Guid correlationId) =>
        Database.KeyDeleteAsync(MachineWaitingKey(correlationId));

    // stations
    private static string StationKey(Guid stationId) => $"stations:{stationId}";

    public Task SetStationAsync(Guid stationId, string name, long expectedCycleMilliseconds, long cycleToleranceMilliseconds) =>
        Database.HashSetAsync(StationKey(stationId),
        [
            new HashEntry("name", name),
            new HashEntry("expected_cycle", expectedCycleMilliseconds),
            new HashEntry("cycle_tolerance", cycleToleranceMilliseconds)
        ]);

    public async Task<(string Name, long ExpectedCycleMilliseconds, long CycleToleranceMilliseconds)?> GetStationAsync(Guid stationId)
    {
        var values = await Database.HashGetAsync(StationKey(stationId), ["name", "expected_cycle", "cycle_tolerance"]);

        if (values[0].IsNullOrEmpty || values[1].IsNullOrEmpty || values[2].IsNullOrEmpty)
        {
            return null;
        }

        return (values[0].ToString(), (long)values[1], (long)values[2]);
    }

    // machines
    private static string MachineKey(Guid machineId) => $"machines:{machineId}";

    public Task SetMachineAsync(Guid machineId, Guid stationId, string name, long minDowntimeMilliseconds, long microStopLimitMilliseconds) =>
        Database.HashSetAsync(MachineKey(machineId),
        [
            new HashEntry("station_id", stationId.ToString()),
            new HashEntry("name", name),
            new HashEntry("min_downtime", minDowntimeMilliseconds),
            new HashEntry("micro_stop_limit", microStopLimitMilliseconds)
        ]);

    public async Task<(Guid StationId, string Name, long MinDowntimeMilliseconds, long MicroStopLimitMilliseconds)?> GetMachineAsync(Guid machineId)
    {
        var values = await Database.HashGetAsync(MachineKey(machineId), ["station_id", "name", "min_downtime", "micro_stop_limit"]);

        if (values[0].IsNullOrEmpty || values[1].IsNullOrEmpty || values[2].IsNullOrEmpty || values[3].IsNullOrEmpty)
        {
            return null;
        }

        return (Guid.Parse(values[0].ToString()), values[1].ToString(), (long)values[2], (long)values[3]);
    }
}
