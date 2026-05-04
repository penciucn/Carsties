using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuctionService.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncItemMileageAndYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Millage",
                table: "Items",
                newName: "Mileage");

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Year",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "Mileage",
                table: "Items",
                newName: "Millage");
        }
    }
}
