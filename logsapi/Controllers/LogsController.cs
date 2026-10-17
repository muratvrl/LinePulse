using LinePulse.LogsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinePulse.LogsApi.Controllers;

[ApiController]
[Route("api/logs")]
public class LogsController(ILogService logService) : ControllerBase
{
    [HttpPost("cycle")]
    public async Task<IActionResult> PostCycleLog(CycleLogRequest request, CancellationToken cancellationToken)
    {
        var cycleLog = await logService.RecordCycleLogAsync(request, cancellationToken);

        return Ok(cycleLog);
    }

    [HttpPost("machine-downtime")]
    public async Task<IActionResult> PostMachineDowntimeLog(MachineDowntimeLogRequest request, CancellationToken cancellationToken)
    {
        var machineDowntimeLog = await logService.RecordMachineDowntimeLogAsync(request, cancellationToken);

        return Ok(machineDowntimeLog);
    }
}
