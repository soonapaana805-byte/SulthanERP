using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sulthan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerBillPrintQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerBillPrintJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsReprint = table.Column<bool>(type: "bit", nullable: false),
                    RequestKey = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
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
                    table.PrimaryKey("PK_CustomerBillPrintJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerBillPrintJobs_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerBillPrintJobs_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBillPrintJobs_OrderId",
                table: "CustomerBillPrintJobs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBillPrintJobs_RequestedByUserId",
                table: "CustomerBillPrintJobs",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBillPrintJobs_RequestKey",
                table: "CustomerBillPrintJobs",
                column: "RequestKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBillPrintJobs_Status_NextAttemptOn",
                table: "CustomerBillPrintJobs",
                columns: new[] { "Status", "NextAttemptOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerBillPrintJobs");
        }
    }
}
