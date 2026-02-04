using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FerroviaApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Length = table.Column<int>(type: "integer", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    IsStop = table.Column<bool>(type: "boolean", nullable: false),
                    PlatformNumber = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Semaphores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsWorking = table.Column<bool>(type: "boolean", nullable: false),
                    IsGreen = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeviating = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semaphores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Length = table.Column<int>(type: "integer", nullable: false),
                    Origin = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Destination = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    TrainType = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    ScheduledArrivalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledDepartureTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsTransit = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedPlatform = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainsLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Length = table.Column<int>(type: "integer", nullable: false),
                    Origin = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Destination = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    TrainType = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    ScheduledArrivalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledDepartureTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsTransit = table.Column<bool>(type: "boolean", nullable: false),
                    ActualDepartureTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualArrivalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedPlatform = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainsLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "switches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InputId = table.Column<int>(type: "integer", nullable: false),
                    StraightId = table.Column<int>(type: "integer", nullable: false),
                    DeviationId = table.Column<int>(type: "integer", nullable: false),
                    SemaphoreId = table.Column<int>(type: "integer", nullable: false),
                    IsWorking = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeviating = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_switches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_switches_Sectors_DeviationId",
                        column: x => x.DeviationId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_switches_Sectors_InputId",
                        column: x => x.InputId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_switches_Sectors_StraightId",
                        column: x => x.StraightId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_switches_Semaphores_SemaphoreId",
                        column: x => x.SemaphoreId,
                        principalTable: "Semaphores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Maintenance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    train_d = table.Column<int>(type: "integer", nullable: false),
                    MaintenanceStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaintenanceEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaintenancePlatform = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maintenance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maintenance_Sectors_MaintenancePlatform",
                        column: x => x.MaintenancePlatform,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Maintenance_TrainsLogs_train_d",
                        column: x => x.train_d,
                        principalTable: "TrainsLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Maintenance_MaintenancePlatform",
                table: "Maintenance",
                column: "MaintenancePlatform");

            migrationBuilder.CreateIndex(
                name: "IX_Maintenance_train_d",
                table: "Maintenance",
                column: "train_d");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_PlatformNumber",
                table: "Sectors",
                column: "PlatformNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_switches_DeviationId",
                table: "switches",
                column: "DeviationId");

            migrationBuilder.CreateIndex(
                name: "IX_switches_InputId",
                table: "switches",
                column: "InputId");

            migrationBuilder.CreateIndex(
                name: "IX_switches_SemaphoreId",
                table: "switches",
                column: "SemaphoreId");

            migrationBuilder.CreateIndex(
                name: "IX_switches_StraightId",
                table: "switches",
                column: "StraightId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Maintenance");

            migrationBuilder.DropTable(
                name: "switches");

            migrationBuilder.DropTable(
                name: "Trains");

            migrationBuilder.DropTable(
                name: "TrainsLogs");

            migrationBuilder.DropTable(
                name: "Sectors");

            migrationBuilder.DropTable(
                name: "Semaphores");
        }
    }
}
