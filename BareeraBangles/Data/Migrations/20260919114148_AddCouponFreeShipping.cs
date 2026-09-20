using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BareeraBangles.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponFreeShipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FreeShipping",
                table: "Coupons",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreeShipping",
                table: "Coupons");
        }
    }
}
