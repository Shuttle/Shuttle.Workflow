using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.Workflow.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class Add_ProcessMessage_Progress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemsCompleted",
                schema: "workflow",
                table: "ProcessMessage",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ItemsTotal",
                schema: "workflow",
                table: "ProcessMessage",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemsCompleted",
                schema: "workflow",
                table: "ProcessMessage");

            migrationBuilder.DropColumn(
                name: "ItemsTotal",
                schema: "workflow",
                table: "ProcessMessage");
        }
    }
}
