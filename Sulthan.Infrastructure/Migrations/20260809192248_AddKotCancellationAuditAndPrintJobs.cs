using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sulthan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKotCancellationAuditAndPrintJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KitchenPrintJobs_KitchenOrderTicketId_KitchenName",
                table: "KitchenPrintJobs");

            migrationBuilder.AddColumn<int>(
                name: "CancelledQuantity",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "KitchenPrintJobs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "OriginalKot");

            migrationBuilder.AddColumn<int>(
                name: "KotCancellationAuditId",
                table: "KitchenPrintJobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledOn",
                table: "KitchenOrderTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "KitchenOrderTickets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<decimal>(
                name: "CancelledQuantity",
                table: "KitchenOrderTicketItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "OrderItemId",
                table: "KitchenOrderTicketItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KotCancellationAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KitchenOrderTicketId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    KotNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BillNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    RequestedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CancelledOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NewStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PreviousSubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviousDiscount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviousTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviousGrandTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewSubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewDiscount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewGrandTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KotCancellationAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KotCancellationAudits_KitchenOrderTickets_KitchenOrderTicketId",
                        column: x => x.KitchenOrderTicketId,
                        principalTable: "KitchenOrderTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KotCancellationAudits_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KotCancellationAudits_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KotCancellationAudits_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KotCancellationAuditItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KotCancellationAuditId = table.Column<int>(type: "int", nullable: false),
                    MenuItemId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    KitchenName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CancelledQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KotCancellationAuditItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KotCancellationAuditItems_KotCancellationAudits_KotCancellationAuditId",
                        column: x => x.KotCancellationAuditId,
                        principalTable: "KotCancellationAudits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KitchenPrintJobs_KitchenOrderTicketId_KitchenName_DocumentType",
                table: "KitchenPrintJobs",
                columns: new[] { "KitchenOrderTicketId", "KitchenName", "DocumentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenPrintJobs_KotCancellationAuditId",
                table: "KitchenPrintJobs",
                column: "KotCancellationAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenOrderTicketItems_OrderItemId",
                table: "KitchenOrderTicketItems",
                column: "OrderItemId",
                unique: true,
                filter: "[OrderItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KotCancellationAuditItems_KotCancellationAuditId",
                table: "KotCancellationAuditItems",
                column: "KotCancellationAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_KotCancellationAudits_ApprovedByUserId",
                table: "KotCancellationAudits",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KotCancellationAudits_CancelledOn",
                table: "KotCancellationAudits",
                column: "CancelledOn");

            migrationBuilder.CreateIndex(
                name: "IX_KotCancellationAudits_KitchenOrderTicketId",
                table: "KotCancellationAudits",
                column: "KitchenOrderTicketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KotCancellationAudits_OrderId",
                table: "KotCancellationAudits",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_KotCancellationAudits_RequestedByUserId",
                table: "KotCancellationAudits",
                column: "RequestedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenOrderTicketItems_OrderItems_OrderItemId",
                table: "KitchenOrderTicketItems",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenPrintJobs_KotCancellationAudits_KotCancellationAuditId",
                table: "KitchenPrintJobs",
                column: "KotCancellationAuditId",
                principalTable: "KotCancellationAudits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KitchenOrderTicketItems_OrderItems_OrderItemId",
                table: "KitchenOrderTicketItems");

            migrationBuilder.DropForeignKey(
                name: "FK_KitchenPrintJobs_KotCancellationAudits_KotCancellationAuditId",
                table: "KitchenPrintJobs");

            migrationBuilder.DropTable(
                name: "KotCancellationAuditItems");

            migrationBuilder.DropTable(
                name: "KotCancellationAudits");

            migrationBuilder.DropIndex(
                name: "IX_KitchenPrintJobs_KitchenOrderTicketId_KitchenName_DocumentType",
                table: "KitchenPrintJobs");

            migrationBuilder.DropIndex(
                name: "IX_KitchenPrintJobs_KotCancellationAuditId",
                table: "KitchenPrintJobs");

            migrationBuilder.DropIndex(
                name: "IX_KitchenOrderTicketItems_OrderItemId",
                table: "KitchenOrderTicketItems");

            migrationBuilder.DropColumn(
                name: "CancelledQuantity",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "KitchenPrintJobs");

            migrationBuilder.DropColumn(
                name: "KotCancellationAuditId",
                table: "KitchenPrintJobs");

            migrationBuilder.DropColumn(
                name: "CancelledOn",
                table: "KitchenOrderTickets");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "KitchenOrderTickets");

            migrationBuilder.DropColumn(
                name: "CancelledQuantity",
                table: "KitchenOrderTicketItems");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "KitchenOrderTicketItems");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenPrintJobs_KitchenOrderTicketId_KitchenName",
                table: "KitchenPrintJobs",
                columns: new[] { "KitchenOrderTicketId", "KitchenName" },
                unique: true);
        }
    }
}
