using Microsoft.EntityFrameworkCore;

namespace LinePulse.LogsApi.Data;

public class LogsDbContext(DbContextOptions<LogsDbContext> options) : DbContext(options)
{
    public DbSet<CycleLog> CycleLogs => Set<CycleLog>();
    public DbSet<MachineDowntimeLog> MachineDowntimeLogs => Set<MachineDowntimeLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CycleLog>(entity =>
        {
            entity.ToTable("cycle_logs");
            entity.HasKey(e => new { e.CorrelationId, e.CycleStart });
            entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
            entity.Property(e => e.StationId).HasColumnName("station_id");
            entity.Property(e => e.StationName).HasColumnName("station_name");
            entity.Property(e => e.CycleStart).HasColumnName("cycle_start");
            entity.Property(e => e.CycleEnd).HasColumnName("cycle_end");
            entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.ExpectedCycleMs).HasColumnName("expected_cycle_ms");
            entity.Property(e => e.ToleranceMs).HasColumnName("tolerance_ms");
        });

        modelBuilder.Entity<MachineDowntimeLog>(entity =>
        {
            entity.ToTable("machine_downtime_logs");
            entity.HasKey(e => new { e.CorrelationId, e.DowntimeStart });
            entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
            entity.Property(e => e.MachineId).HasColumnName("machine_id");
            entity.Property(e => e.MachineName).HasColumnName("machine_name");
            entity.Property(e => e.StationId).HasColumnName("station_id");
            entity.Property(e => e.StationName).HasColumnName("station_name");
            entity.Property(e => e.DowntimeStart).HasColumnName("downtime_start");
            entity.Property(e => e.DowntimeEnd).HasColumnName("downtime_end");
            entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.MinDowntimeMs).HasColumnName("min_downtime_ms");
            entity.Property(e => e.MicroStopLimitMs).HasColumnName("micro_stop_limit_ms");
        });
    }
}

public class CycleLog
{
    public Guid CorrelationId { get; set; }
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public DateTimeOffset CycleStart { get; set; }
    public DateTimeOffset CycleEnd { get; set; }
    public long DurationMs { get; set; }
    public string Status { get; set; } = string.Empty;
    public long ExpectedCycleMs { get; set; }
    public long ToleranceMs { get; set; }
}

public class MachineDowntimeLog
{
    public Guid CorrelationId { get; set; }
    public Guid MachineId { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public DateTimeOffset DowntimeStart { get; set; }
    public DateTimeOffset DowntimeEnd { get; set; }
    public long DurationMs { get; set; }
    public string Status { get; set; } = string.Empty;
    public long MinDowntimeMs { get; set; }
    public long MicroStopLimitMs { get; set; }
}
