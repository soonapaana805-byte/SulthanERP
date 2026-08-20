using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sulthan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenPrintQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KitchenPrintJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KitchenOrderTicketId = table.Column<int>(type: "int", nullable: false),
                    KitchenName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PrinterName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Completed"),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastAttemptOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextAttemptOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenPrintJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenPrintJobs_KitchenOrderTickets_KitchenOrderTicketId",
                        column: x => x.KitchenOrderTicketId,
                        principalTable: "KitchenOrderTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KitchenPrintJobs_KitchenOrderTicketId_KitchenName",
                table: "KitchenPrintJobs",
                columns: new[] { "KitchenOrderTicketId", "KitchenName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenPrintJobs_Status_NextAttemptOn",
                table: "KitchenPrintJobs",
                columns: new[] { "Status", "NextAttemptOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KitchenPrintJobs");
        }
    }
}
