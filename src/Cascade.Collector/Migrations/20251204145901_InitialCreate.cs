using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cascade.Collector.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Connections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceEndpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetEndpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MessageTypeShort = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MessageCount = table.Column<long>(type: "bigint", nullable: false),
                    FailureCount = table.Column<long>(type: "bigint", nullable: false),
                    FirstSeen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TotalLatencyMs = table.Column<double>(type: "float", nullable: false),
                    LatencyCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Endpoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FirstSeen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MessagesReceived = table.Column<long>(type: "bigint", nullable: false),
                    MessagesSent = table.Column<long>(type: "bigint", nullable: false),
                    Failures = table.Column<long>(type: "bigint", nullable: false),
                    TotalProcessingTimeMs = table.Column<double>(type: "float", nullable: false),
                    ProcessingTimeCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Endpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConversationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CausationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedTo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MessageType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MessageTypeShort = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EndpointName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HostId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessingDuration = table.Column<TimeSpan>(type: "time", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: true),
                    ExceptionType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExceptionMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OriginatingEndpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SagaId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SagaType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Connections_SourceEndpoint_TargetEndpoint_MessageType",
                table: "Connections",
                columns: new[] { "SourceEndpoint", "TargetEndpoint", "MessageType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Endpoints_Name",
                table: "Endpoints",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CorrelationId",
                table: "Messages",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CorrelationId_Timestamp",
                table: "Messages",
                columns: new[] { "CorrelationId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CreatedAt",
                table: "Messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_EndpointName",
                table: "Messages",
                column: "EndpointName");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Timestamp",
                table: "Messages",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Connections");

            migrationBuilder.DropTable(
                name: "Endpoints");

            migrationBuilder.DropTable(
                name: "Messages");
        }
    }
}
