using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FerroviaApi.Migrations
{
    /// <inheritdoc />
    public partial class Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Maintenance_Sectors_MaintenancePlatform",
                table: "Maintenance");

            migrationBuilder.DropForeignKey(
                name: "FK_Maintenance_TrainsLogs_train_id",
                table: "Maintenance");

            migrationBuilder.DropForeignKey(
                name: "FK_switches_Sectors_DeviationId",
                table: "switches");

            migrationBuilder.DropForeignKey(
                name: "FK_switches_Sectors_InputId",
                table: "switches");

            migrationBuilder.DropForeignKey(
                name: "FK_switches_Sectors_StraightId",
                table: "switches");

            migrationBuilder.DropForeignKey(
                name: "FK_switches_Semaphores_SemaphoreId",
                table: "switches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Trains",
                table: "Trains");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Semaphores",
                table: "Semaphores");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sectors",
                table: "Sectors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Maintenance",
                table: "Maintenance");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainsLogs",
                table: "TrainsLogs");

            migrationBuilder.RenameTable(
                name: "Trains",
                newName: "trains");

            migrationBuilder.RenameTable(
                name: "Semaphores",
                newName: "semaphores");

            migrationBuilder.RenameTable(
                name: "Sectors",
                newName: "sectors");

            migrationBuilder.RenameTable(
                name: "Maintenance",
                newName: "maintenance");

            migrationBuilder.RenameTable(
                name: "TrainsLogs",
                newName: "trains_log");

            migrationBuilder.RenameColumn(
                name: "Origin",
                table: "trains",
                newName: "origin");

            migrationBuilder.RenameColumn(
                name: "Length",
                table: "trains",
                newName: "length");

            migrationBuilder.RenameColumn(
                name: "Destination",
                table: "trains",
                newName: "destination");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "trains",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TrainType",
                table: "trains",
                newName: "train_type");

            migrationBuilder.RenameColumn(
                name: "TrainNumber",
                table: "trains",
                newName: "train_number");

            migrationBuilder.RenameColumn(
                name: "ScheduledDepartureTime",
                table: "trains",
                newName: "scheduled_departure_time");

            migrationBuilder.RenameColumn(
                name: "ScheduledArrivalTime",
                table: "trains",
                newName: "scheduled_arrival_time");

            migrationBuilder.RenameColumn(
                name: "IsTransit",
                table: "trains",
                newName: "is_transit");

            migrationBuilder.RenameColumn(
                name: "AssignedPlatform",
                table: "trains",
                newName: "assigned_platform");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "switches",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "StraightId",
                table: "switches",
                newName: "straight_id");

            migrationBuilder.RenameColumn(
                name: "SemaphoreId",
                table: "switches",
                newName: "semaphore_id");

            migrationBuilder.RenameColumn(
                name: "IsWorking",
                table: "switches",
                newName: "is_working");

            migrationBuilder.RenameColumn(
                name: "IsDeviating",
                table: "switches",
                newName: "is_deviating");

            migrationBuilder.RenameColumn(
                name: "InputId",
                table: "switches",
                newName: "input_id");

            migrationBuilder.RenameColumn(
                name: "DeviationId",
                table: "switches",
                newName: "deviation_id");

            migrationBuilder.RenameIndex(
                name: "IX_switches_StraightId",
                table: "switches",
                newName: "IX_switches_straight_id");

            migrationBuilder.RenameIndex(
                name: "IX_switches_SemaphoreId",
                table: "switches",
                newName: "IX_switches_semaphore_id");

            migrationBuilder.RenameIndex(
                name: "IX_switches_InputId",
                table: "switches",
                newName: "IX_switches_input_id");

            migrationBuilder.RenameIndex(
                name: "IX_switches_DeviationId",
                table: "switches",
                newName: "IX_switches_deviation_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "semaphores",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "IsWorking",
                table: "semaphores",
                newName: "is_working");

            migrationBuilder.RenameColumn(
                name: "IsGreen",
                table: "semaphores",
                newName: "is_green");

            migrationBuilder.RenameColumn(
                name: "IsDeviating",
                table: "semaphores",
                newName: "is_deviating");

            migrationBuilder.RenameColumn(
                name: "Length",
                table: "sectors",
                newName: "length");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "sectors",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PlatformNumber",
                table: "sectors",
                newName: "platform_number");

            migrationBuilder.RenameColumn(
                name: "IsStop",
                table: "sectors",
                newName: "is_stop");

            migrationBuilder.RenameColumn(
                name: "IsAvailable",
                table: "sectors",
                newName: "is_available");

            migrationBuilder.RenameIndex(
                name: "IX_Sectors_PlatformNumber",
                table: "sectors",
                newName: "IX_sectors_platform_number");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "maintenance",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "MaintenanceStart",
                table: "maintenance",
                newName: "maintenance_start");

            migrationBuilder.RenameColumn(
                name: "MaintenancePlatform",
                table: "maintenance",
                newName: "maintenance_platform");

            migrationBuilder.RenameColumn(
                name: "MaintenanceEnd",
                table: "maintenance",
                newName: "maintenance_end");

            migrationBuilder.RenameIndex(
                name: "IX_Maintenance_train_id",
                table: "maintenance",
                newName: "IX_maintenance_train_id");

            migrationBuilder.RenameIndex(
                name: "IX_Maintenance_MaintenancePlatform",
                table: "maintenance",
                newName: "IX_maintenance_maintenance_platform");

            migrationBuilder.RenameColumn(
                name: "Origin",
                table: "trains_log",
                newName: "origin");

            migrationBuilder.RenameColumn(
                name: "Length",
                table: "trains_log",
                newName: "length");

            migrationBuilder.RenameColumn(
                name: "Destination",
                table: "trains_log",
                newName: "destination");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "trains_log",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TrainType",
                table: "trains_log",
                newName: "train_type");

            migrationBuilder.RenameColumn(
                name: "TrainNumber",
                table: "trains_log",
                newName: "train_number");

            migrationBuilder.RenameColumn(
                name: "ScheduledDepartureTime",
                table: "trains_log",
                newName: "scheduled_departure_time");

            migrationBuilder.RenameColumn(
                name: "ScheduledArrivalTime",
                table: "trains_log",
                newName: "scheduled_arrival_time");

            migrationBuilder.RenameColumn(
                name: "IsTransit",
                table: "trains_log",
                newName: "is_transit");

            migrationBuilder.RenameColumn(
                name: "AssignedPlatform",
                table: "trains_log",
                newName: "assigned_platform");

            migrationBuilder.RenameColumn(
                name: "ActualDepartureTime",
                table: "trains_log",
                newName: "actual_departure_time");

            migrationBuilder.RenameColumn(
                name: "ActualArrivalTime",
                table: "trains_log",
                newName: "actual_arrival_time");

            migrationBuilder.AddPrimaryKey(
                name: "PK_trains",
                table: "trains",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_semaphores",
                table: "semaphores",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sectors",
                table: "sectors",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_maintenance",
                table: "maintenance",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_trains_log",
                table: "trains_log",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_maintenance_sectors_maintenance_platform",
                table: "maintenance",
                column: "maintenance_platform",
                principalTable: "sectors",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_maintenance_trains_log_train_id",
                table: "maintenance",
                column: "train_id",
                principalTable: "trains_log",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_switches_sectors_deviation_id",
                table: "switches",
                column: "deviation_id",
                principalTable: "sectors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_switches_sectors_input_id",
                table: "switches",
                column: "input_id",
                principalTable: "sectors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_switches_sectors_straight_id",
                table: "switches",
                column: "straight_id",
                principalTable: "sectors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_switches_semaphores_semaphore_id",
                table: "switches",
                column: "semaphore_id",
                principalTable: "semaphores",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_maintenance_sectors_maintenance_platform",
                table: "maintenance");

            migrationBuilder.DropForeignKey(
                name: "FK_maintenance_trains_log_train_id",
                table: "maintenance");

            migrationBuilder.DropForeignKey(
                name: "FK_switches_sectors_deviation_id",
                table: "switches");

            migrationBuilder.DropForeignKey(
                name: "FK_switches_sectors_input_id",
                table: "switches");

            migrationBuilder.DropForeignKey(
                name: "FK_switches_sectors_straight_id",
                table: "switches");

            migrationBuilder.DropForeignKey(
                name: "FK_switches_semaphores_semaphore_id",
                table: "switches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_trains",
                table: "trains");

            migrationBuilder.DropPrimaryKey(
                name: "PK_semaphores",
                table: "semaphores");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sectors",
                table: "sectors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_maintenance",
                table: "maintenance");

            migrationBuilder.DropPrimaryKey(
                name: "PK_trains_log",
                table: "trains_log");

            migrationBuilder.RenameTable(
                name: "trains",
                newName: "Trains");

            migrationBuilder.RenameTable(
                name: "semaphores",
                newName: "Semaphores");

            migrationBuilder.RenameTable(
                name: "sectors",
                newName: "Sectors");

            migrationBuilder.RenameTable(
                name: "maintenance",
                newName: "Maintenance");

            migrationBuilder.RenameTable(
                name: "trains_log",
                newName: "TrainsLogs");

            migrationBuilder.RenameColumn(
                name: "origin",
                table: "Trains",
                newName: "Origin");

            migrationBuilder.RenameColumn(
                name: "length",
                table: "Trains",
                newName: "Length");

            migrationBuilder.RenameColumn(
                name: "destination",
                table: "Trains",
                newName: "Destination");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Trains",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "train_type",
                table: "Trains",
                newName: "TrainType");

            migrationBuilder.RenameColumn(
                name: "train_number",
                table: "Trains",
                newName: "TrainNumber");

            migrationBuilder.RenameColumn(
                name: "scheduled_departure_time",
                table: "Trains",
                newName: "ScheduledDepartureTime");

            migrationBuilder.RenameColumn(
                name: "scheduled_arrival_time",
                table: "Trains",
                newName: "ScheduledArrivalTime");

            migrationBuilder.RenameColumn(
                name: "is_transit",
                table: "Trains",
                newName: "IsTransit");

            migrationBuilder.RenameColumn(
                name: "assigned_platform",
                table: "Trains",
                newName: "AssignedPlatform");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "switches",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "straight_id",
                table: "switches",
                newName: "StraightId");

            migrationBuilder.RenameColumn(
                name: "semaphore_id",
                table: "switches",
                newName: "SemaphoreId");

            migrationBuilder.RenameColumn(
                name: "is_working",
                table: "switches",
                newName: "IsWorking");

            migrationBuilder.RenameColumn(
                name: "is_deviating",
                table: "switches",
                newName: "IsDeviating");

            migrationBuilder.RenameColumn(
                name: "input_id",
                table: "switches",
                newName: "InputId");

            migrationBuilder.RenameColumn(
                name: "deviation_id",
                table: "switches",
                newName: "DeviationId");

            migrationBuilder.RenameIndex(
                name: "IX_switches_straight_id",
                table: "switches",
                newName: "IX_switches_StraightId");

            migrationBuilder.RenameIndex(
                name: "IX_switches_semaphore_id",
                table: "switches",
                newName: "IX_switches_SemaphoreId");

            migrationBuilder.RenameIndex(
                name: "IX_switches_input_id",
                table: "switches",
                newName: "IX_switches_InputId");

            migrationBuilder.RenameIndex(
                name: "IX_switches_deviation_id",
                table: "switches",
                newName: "IX_switches_DeviationId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Semaphores",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "is_working",
                table: "Semaphores",
                newName: "IsWorking");

            migrationBuilder.RenameColumn(
                name: "is_green",
                table: "Semaphores",
                newName: "IsGreen");

            migrationBuilder.RenameColumn(
                name: "is_deviating",
                table: "Semaphores",
                newName: "IsDeviating");

            migrationBuilder.RenameColumn(
                name: "length",
                table: "Sectors",
                newName: "Length");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Sectors",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "platform_number",
                table: "Sectors",
                newName: "PlatformNumber");

            migrationBuilder.RenameColumn(
                name: "is_stop",
                table: "Sectors",
                newName: "IsStop");

            migrationBuilder.RenameColumn(
                name: "is_available",
                table: "Sectors",
                newName: "IsAvailable");

            migrationBuilder.RenameIndex(
                name: "IX_sectors_platform_number",
                table: "Sectors",
                newName: "IX_Sectors_PlatformNumber");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Maintenance",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "maintenance_start",
                table: "Maintenance",
                newName: "MaintenanceStart");

            migrationBuilder.RenameColumn(
                name: "maintenance_platform",
                table: "Maintenance",
                newName: "MaintenancePlatform");

            migrationBuilder.RenameColumn(
                name: "maintenance_end",
                table: "Maintenance",
                newName: "MaintenanceEnd");

            migrationBuilder.RenameIndex(
                name: "IX_maintenance_train_id",
                table: "Maintenance",
                newName: "IX_Maintenance_train_id");

            migrationBuilder.RenameIndex(
                name: "IX_maintenance_maintenance_platform",
                table: "Maintenance",
                newName: "IX_Maintenance_MaintenancePlatform");

            migrationBuilder.RenameColumn(
                name: "origin",
                table: "TrainsLogs",
                newName: "Origin");

            migrationBuilder.RenameColumn(
                name: "length",
                table: "TrainsLogs",
                newName: "Length");

            migrationBuilder.RenameColumn(
                name: "destination",
                table: "TrainsLogs",
                newName: "Destination");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "TrainsLogs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "train_type",
                table: "TrainsLogs",
                newName: "TrainType");

            migrationBuilder.RenameColumn(
                name: "train_number",
                table: "TrainsLogs",
                newName: "TrainNumber");

            migrationBuilder.RenameColumn(
                name: "scheduled_departure_time",
                table: "TrainsLogs",
                newName: "ScheduledDepartureTime");

            migrationBuilder.RenameColumn(
                name: "scheduled_arrival_time",
                table: "TrainsLogs",
                newName: "ScheduledArrivalTime");

            migrationBuilder.RenameColumn(
                name: "is_transit",
                table: "TrainsLogs",
                newName: "IsTransit");

            migrationBuilder.RenameColumn(
                name: "assigned_platform",
                table: "TrainsLogs",
                newName: "AssignedPlatform");

            migrationBuilder.RenameColumn(
                name: "actual_departure_time",
                table: "TrainsLogs",
                newName: "ActualDepartureTime");

            migrationBuilder.RenameColumn(
                name: "actual_arrival_time",
                table: "TrainsLogs",
                newName: "ActualArrivalTime");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trains",
                table: "Trains",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Semaphores",
                table: "Semaphores",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sectors",
                table: "Sectors",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Maintenance",
                table: "Maintenance",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainsLogs",
                table: "TrainsLogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenance_Sectors_MaintenancePlatform",
                table: "Maintenance",
                column: "MaintenancePlatform",
                principalTable: "Sectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenance_TrainsLogs_train_id",
                table: "Maintenance",
                column: "train_id",
                principalTable: "TrainsLogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_switches_Sectors_DeviationId",
                table: "switches",
                column: "DeviationId",
                principalTable: "Sectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_switches_Sectors_InputId",
                table: "switches",
                column: "InputId",
                principalTable: "Sectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_switches_Sectors_StraightId",
                table: "switches",
                column: "StraightId",
                principalTable: "Sectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_switches_Semaphores_SemaphoreId",
                table: "switches",
                column: "SemaphoreId",
                principalTable: "Semaphores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
