using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.Workflow.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workflow");

            migrationBuilder.CreateTable(
                name: "Process",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DateRegistered = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DateCompleted = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeferredTill = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OverdueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(130)", maxLength: 130, nullable: false),
                    StatusMessage = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ContinuationToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContinuationMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContinuationRegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Process", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessDefinition",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessDefinition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceType",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(130)", maxLength: 130, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Semaphore",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DateRegistered = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semaphore", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "State",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_State", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessCommit",
                schema: "workflow",
                columns: table => new
                {
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    DateCommitted = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessCommit", x => new { x.ProcessId, x.Key });
                    table.ForeignKey(
                        name: "FK_ProcessCommit_Process_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "workflow",
                        principalTable: "Process",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessMessage",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    DateSent = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DateCompleted = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InvokeTimeout = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessMessage_Process_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "workflow",
                        principalTable: "Process",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessDefinitionMessage",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    InvokeTimeout = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessDefinitionMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessDefinitionMessage_ProcessDefinition_ProcessDefinitionId",
                        column: x => x.ProcessDefinitionId,
                        principalSchema: "workflow",
                        principalTable: "ProcessDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessDefinitionStateItem",
                schema: "workflow",
                columns: table => new
                {
                    ProcessDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(130)", maxLength: 130, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessDefinitionStateItem", x => new { x.ProcessDefinitionId, x.Name });
                    table.ForeignKey(
                        name: "FK_ProcessDefinitionStateItem_ProcessDefinition_ProcessDefinitionId",
                        column: x => x.ProcessDefinitionId,
                        principalSchema: "workflow",
                        principalTable: "ProcessDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceItem",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(130)", maxLength: 130, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceItem_ReferenceType_ReferenceTypeId",
                        column: x => x.ReferenceTypeId,
                        principalSchema: "workflow",
                        principalTable: "ReferenceType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateItem",
                schema: "workflow",
                columns: table => new
                {
                    StateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(130)", maxLength: 130, nullable: false),
                    EffectiveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValue: new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0))),
                    Type = table.Column<int>(type: "int", nullable: false),
                    StringValue = table.Column<string>(type: "nvarchar(max)", maxLength: 2147483647, nullable: true),
                    DateTimeValue = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecimalValue = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    GuidValue = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveDateEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValue: new DateTimeOffset(new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999), new TimeSpan(0, 0, 0, 0, 0))),
                    DateRegistered = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "TODATETIMEOFFSET(SYSUTCDATETIME(), 0)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateItem", x => new { x.StateId, x.Name, x.EffectiveDate });
                    table.ForeignKey(
                        name: "FK_StateItem_State_StateId",
                        column: x => x.StateId,
                        principalSchema: "workflow",
                        principalTable: "State",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Process_Key_DateCompleted",
                schema: "workflow",
                table: "Process",
                columns: new[] { "Key", "DateCompleted" },
                filter: "[Key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Process_Key_Status",
                schema: "workflow",
                table: "Process",
                columns: new[] { "Key", "Status" },
                filter: "[Key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessDefinition",
                schema: "workflow",
                table: "ProcessDefinition",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessDefinition",
                schema: "workflow",
                table: "ProcessDefinitionMessage",
                columns: new[] { "ProcessDefinitionId", "TypeName", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessMessage",
                schema: "workflow",
                table: "ProcessMessage",
                columns: new[] { "ProcessId", "TypeName", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceItem_ReferenceTypeId",
                schema: "workflow",
                table: "ReferenceItem",
                column: "ReferenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Semaphore",
                schema: "workflow",
                table: "Semaphore",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_State",
                schema: "workflow",
                table: "State",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessCommit",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "ProcessDefinitionMessage",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "ProcessDefinitionStateItem",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "ProcessMessage",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "ReferenceItem",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "Semaphore",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "StateItem",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "ProcessDefinition",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "Process",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "ReferenceType",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "State",
                schema: "workflow");
        }
    }
}
