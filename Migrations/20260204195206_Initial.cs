using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRAttendanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ScanOpen",
                table: "Sessions",
                newName: "PresentUntil");

            migrationBuilder.RenameColumn(
                name: "LateAfter",
                table: "Sessions",
                newName: "LateUntil");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PresentUntil",
                table: "Sessions",
                newName: "ScanOpen");

            migrationBuilder.RenameColumn(
                name: "LateUntil",
                table: "Sessions",
                newName: "LateAfter");
        }
    }
}
