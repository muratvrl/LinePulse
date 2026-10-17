using System.ComponentModel.DataAnnotations;
using LinePulse.LogsApi.Data;
using LinePulse.LogsApi.Repositories;

namespace LinePulse.LogsApi.Services;

public interface ILogService
{
    Task<CycleLog> RecordCycleLogAsync(CycleLogRequest request, CancellationToken cancellationToken);
    Task<MachineDowntimeLog> RecordMachineDowntimeLogAsync(MachineDowntimeLogRequest request, CancellationToken cancellationToken);
}

public class LogService(ILogRepository repository) : ILogService
{
    public Task<CycleLog> RecordCycleLogAsync(CycleLogRequest request, CancellationToken cancellationToken) =>
        repository.AddCycleLogAsync(new CycleLog
        {
            CorrelationId = request.CorrelationId,
            StationId = request.StationId,
            StationName = request.StationName,
            CycleStart = request.CycleStart,
            CycleEnd = request.CycleEnd,
            DurationMs = request.DurationMs,
            Status = request.Status,
            ExpectedCycleMs = request.ExpectedCycleMs,
            ToleranceMs = request.ToleranceMs
        }, cancellationToken);

    public Task<MachineDowntimeLog> RecordMachineDowntimeLogAsync(MachineDowntimeLogRequest request, CancellationToken cancellationToken) =>
        repository.AddMachineDowntimeLogAsync(new MachineDowntimeLog
        {
            CorrelationId = request.CorrelationId,
            MachineId = request.MachineId,
            MachineName = request.MachineName,
            StationId = request.StationId,
            StationName = request.StationName,
            DowntimeStart = request.DowntimeStart,
            DowntimeEnd = request.DowntimeEnd,
            DurationMs = request.DurationMs,
            Status = request.Status,
            MinDowntimeMs = request.MinDowntimeMs,
            MicroStopLimitMs = request.MicroStopLimitMs
        }, cancellationToken);
}

//DTOs
public record CycleLogRequest(
    Guid CorrelationId,
    Guid StationId,
    string StationName,
    DateTimeOffset CycleStart,
    DateTimeOffset CycleEnd,
    [Range(0, long.MaxValue)] long DurationMs,
    string Status,
    [Range(0, long.MaxValue)] long ExpectedCycleMs,
    [Range(0, long.MaxValue)] long ToleranceMs);

public record MachineDowntimeLogRequest(
    Guid CorrelationId,
    Guid MachineId,
    string MachineName,
    Guid StationId,
    string StationName,
    DateTimeOffset DowntimeStart,
    DateTimeOffset DowntimeEnd,
    [Range(0, long.MaxValue)] long DurationMs,
    string Status,
    [Range(0, long.MaxValue)] long MinDowntimeMs,
    [Range(0, long.MaxValue)] long MicroStopLimitMs);
