using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sulthan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBillCancellationVoidAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillActionAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    BillNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: false),
                    ActionOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousOrderStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NewOrderStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PreviousPaymentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NewPaymentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FinancialAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviousTableStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NewTableStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillActionAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillActionAudits_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillActionAudits_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillActionAudits_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillActionAudits_ActionOn",
                table: "BillActionAudits",
                column: "ActionOn");

            migrationBuilder.CreateIndex(
                name: "IX_BillActionAudits_ActionType",
                table: "BillActionAudits",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_BillActionAudits_ApprovedByUserId",
                table: "BillActionAudits",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BillActionAudits_OrderId",
                table: "BillActionAudits",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillActionAudits_RequestedByUserId",
                table: "BillActionAudits",
                column: "RequestedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillActionAudits");
        }
    }
}
