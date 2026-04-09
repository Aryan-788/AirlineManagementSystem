using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DealerBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DealerId = table.Column<int>(type: "int", nullable: false),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    FlightId = table.Column<int>(type: "int", nullable: false),
                    Commission = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerBookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dealers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DealerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DealerEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AllocatedSeats = table.Column<int>(type: "int", nullable: false),
                    UsedSeats = table.Column<int>(type: "int", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DealerBookings_BookingId",
                table: "DealerBookings",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_DealerBookings_DealerId",
                table: "DealerBookings",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_Dealers_DealerEmail",
                table: "Dealers",
                column: "DealerEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DealerBookings");

            migrationBuilder.DropTable(
                name: "Dealers");
        }
    }
}
