using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookingService.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundPolicyAndRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefundPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinHoursBeforeDeparture = table.Column<double>(type: "float", nullable: false),
                    MaxHoursBeforeDeparture = table.Column<double>(type: "float", nullable: false),
                    RefundPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    PassengerId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CancellationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DepartureTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefundStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RefundPolicies",
                columns: new[] { "Id", "CreatedAt", "MaxHoursBeforeDeparture", "MinHoursBeforeDeparture", "RefundPercentage", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), 99999.0, 24.0, 100m, null },
                    { 2, new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), 24.0, 2.0, 70m, null },
                    { 3, new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2.0, 0.0, 30m, null },
                    { 4, new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.0, -99999.0, 0m, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_BookingId",
                table: "Refunds",
                column: "BookingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefundPolicies");

            migrationBuilder.DropTable(
                name: "Refunds");
        }
    }
}
