using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinePulse.LogsApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cycle_logs",
                columns: table => new
                {
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cycle_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_name = table.Column<string>(type: "text", nullable: false),
                    cycle_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    expected_cycle_ms = table.Column<long>(type: "bigint", nullable: false),
                    tolerance_ms = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cycle_logs", x => new { x.correlation_id, x.cycle_start });
                });

            migrationBuilder.CreateTable(
                name: "machine_downtime_logs",
                columns: table => new
                {
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    downtime_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    machine_id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_name = table.Column<string>(type: "text", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_name = table.Column<string>(type: "text", nullable: false),
                    downtime_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    min_downtime_ms = table.Column<long>(type: "bigint", nullable: false),
                    micro_stop_limit_ms = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_downtime_logs", x => new { x.correlation_id, x.downtime_start });
                });

            migrationBuilder.Sql(
                "SELECT create_hypertable('cycle_logs', 'cycle_start', if_not_exists => TRUE, create_default_indexes => FALSE);");
            migrationBuilder.Sql(
                "CREATE INDEX cycle_logs_cycle_start_idx ON cycle_logs (cycle_start DESC);");

            migrationBuilder.Sql(
                "SELECT create_hypertable('machine_downtime_logs', 'downtime_start', if_not_exists => TRUE, create_default_indexes => FALSE);");
            migrationBuilder.Sql(
                "CREATE INDEX machine_downtime_logs_downtime_start_idx ON machine_downtime_logs (downtime_start DESC);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cycle_logs");

            migrationBuilder.DropTable(
                name: "machine_downtime_logs");
        }
    }
}
