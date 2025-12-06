using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cascade.Collector.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Intent",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Intent",
                table: "Messages");
        }
    }
}
