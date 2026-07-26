using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sulthan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoPrintAfterPayment",
                table: "Settings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CurrencySymbol",
                table: "Settings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DecimalPlaces",
                table: "Settings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Settings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterMessage",
                table: "Settings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GstNumber",
                table: "Settings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderMessage",
                table: "Settings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsRestaurantOpen",
                table: "Settings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PrinterWidth",
                table: "Settings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ShowGstNumberOnBill",
                table: "Settings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowShopAddressOnBill",
                table: "Settings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowShopPhoneOnBill",
                table: "Settings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowTaxOnCustomerBill",
                table: "Settings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Settings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Settings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoPrintAfterPayment",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CurrencySymbol",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "DecimalPlaces",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "FooterMessage",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "GstNumber",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "HeaderMessage",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "IsRestaurantOpen",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "PrinterWidth",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ShowGstNumberOnBill",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ShowShopAddressOnBill",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ShowShopPhoneOnBill",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ShowTaxOnCustomerBill",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Settings");
        }
    }
}
