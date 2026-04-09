using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.Migrations
{
    /// <inheritdoc />
    public partial class addecolms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PassengerEmail",
                table: "Passengers");

            migrationBuilder.RenameColumn(
                name: "PassengerPhone",
                table: "Passengers",
                newName: "Gender");

            migrationBuilder.RenameColumn(
                name: "PassengerName",
                table: "Passengers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "PassengerAge",
                table: "Passengers",
                newName: "Age");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Passengers",
                newName: "PassengerName");

            migrationBuilder.RenameColumn(
                name: "Gender",
                table: "Passengers",
                newName: "PassengerPhone");

            migrationBuilder.RenameColumn(
                name: "Age",
                table: "Passengers",
                newName: "PassengerAge");

            migrationBuilder.AddColumn<string>(
                name: "PassengerEmail",
                table: "Passengers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
