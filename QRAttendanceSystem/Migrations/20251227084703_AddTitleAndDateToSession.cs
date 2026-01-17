using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRAttendanceSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTitleAndDateToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Sessions");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "Sessions",
                newName: "Date");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Sessions");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Sessions",
                newName: "StartTime");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Sessions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
