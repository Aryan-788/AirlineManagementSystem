using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaggageService.Migrations
{
    /// <inheritdoc />
    public partial class addedcolmns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlightNumber",
                table: "Baggages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PassengerName",
                table: "Baggages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlightNumber",
                table: "Baggages");

            migrationBuilder.DropColumn(
                name: "PassengerName",
                table: "Baggages");
        }
    }
}
