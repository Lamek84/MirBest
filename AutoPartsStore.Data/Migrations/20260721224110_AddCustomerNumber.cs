using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CustomerNumber",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerNumber",
                table: "AspNetUsers");
        }
    }
}
