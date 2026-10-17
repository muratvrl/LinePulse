using LinePulse.LogsApi.Data;
using Microsoft.EntityFrameworkCore;

namespace LinePulse.LogsApi.Repositories;

public interface ILogRepository
{
    Task<CycleLog> AddCycleLogAsync(CycleLog cycleLog, CancellationToken cancellationToken);
    Task<MachineDowntimeLog> AddMachineDowntimeLogAsync(MachineDowntimeLog machineDowntimeLog, CancellationToken cancellationToken);
    Task<List<CycleLog>> GetCycleLogsAsync(CancellationToken cancellationToken);
    Task<List<MachineDowntimeLog>> GetMachineDowntimeLogsAsync(CancellationToken cancellationToken);
}

public class LogRepository(LogsDbContext dbContext) : ILogRepository
{
    public async Task<CycleLog> AddCycleLogAsync(CycleLog cycleLog, CancellationToken cancellationToken)
    {
        dbContext.CycleLogs.Add(cycleLog);
        await dbContext.SaveChangesAsync(cancellationToken);
        return cycleLog;
    }

    public async Task<MachineDowntimeLog> AddMachineDowntimeLogAsync(MachineDowntimeLog machineDowntimeLog, CancellationToken cancellationToken)
    {
        dbContext.MachineDowntimeLogs.Add(machineDowntimeLog);
        await dbContext.SaveChangesAsync(cancellationToken);
        return machineDowntimeLog;
    }

    public Task<List<CycleLog>> GetCycleLogsAsync(CancellationToken cancellationToken) =>
        dbContext.CycleLogs.ToListAsync(cancellationToken);

    public Task<List<MachineDowntimeLog>> GetMachineDowntimeLogsAsync(CancellationToken cancellationToken) =>
        dbContext.MachineDowntimeLogs.ToListAsync(cancellationToken);
}
