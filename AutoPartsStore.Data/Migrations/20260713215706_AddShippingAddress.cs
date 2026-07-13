using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShippingCity",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingName",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingPhone",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingPostalCode",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingStreet",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingCity",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingPhone",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingPostalCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingStreet",
                table: "Orders");
        }
    }
}
