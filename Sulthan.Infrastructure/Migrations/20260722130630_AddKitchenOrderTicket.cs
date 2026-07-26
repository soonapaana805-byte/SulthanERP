using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sulthan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenOrderTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KitchenOrderTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KotNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PrintedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsReprint = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenOrderTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenOrderTickets_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KitchenOrderTicketItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KitchenOrderTicketId = table.Column<int>(type: "int", nullable: false),
                    MenuItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenOrderTicketItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenOrderTicketItems_KitchenOrderTickets_KitchenOrderTicketId",
                        column: x => x.KitchenOrderTicketId,
                        principalTable: "KitchenOrderTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KitchenOrderTicketItems_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KitchenOrderTicketItems_KitchenOrderTicketId",
                table: "KitchenOrderTicketItems",
                column: "KitchenOrderTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenOrderTicketItems_MenuItemId",
                table: "KitchenOrderTicketItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenOrderTickets_KotNumber",
                table: "KitchenOrderTickets",
                column: "KotNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitchenOrderTickets_OrderId",
                table: "KitchenOrderTickets",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KitchenOrderTicketItems");

            migrationBuilder.DropTable(
                name: "KitchenOrderTickets");
        }
    }
}
