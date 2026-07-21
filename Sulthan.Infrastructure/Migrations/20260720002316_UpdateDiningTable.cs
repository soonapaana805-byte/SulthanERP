using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sulthan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDiningTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiningTables_TableCode",
                table: "DiningTables");

            migrationBuilder.DropColumn(
                name: "IsAc",
                table: "DiningTables");

            migrationBuilder.DropColumn(
                name: "IsOccupied",
                table: "DiningTables");

            migrationBuilder.DropColumn(
                name: "TableCode",
                table: "DiningTables");

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "DiningTables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DiningTables",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TableNumber",
                table: "DiningTables",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TableType",
                table: "DiningTables",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DiningTables_TableNumber",
                table: "DiningTables",
                column: "TableNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiningTables_TableNumber",
                table: "DiningTables");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "DiningTables");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DiningTables");

            migrationBuilder.DropColumn(
                name: "TableNumber",
                table: "DiningTables");

            migrationBuilder.DropColumn(
                name: "TableType",
                table: "DiningTables");

            migrationBuilder.AddColumn<bool>(
                name: "IsAc",
                table: "DiningTables",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOccupied",
                table: "DiningTables",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TableCode",
                table: "DiningTables",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DiningTables_TableCode",
                table: "DiningTables",
                column: "TableCode",
                unique: true);
        }
    }
}
