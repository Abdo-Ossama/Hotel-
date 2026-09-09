using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hotel_Booking.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponFieldsToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppliedCouponCode",
                table: "Bookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AppliedCouponId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AppliedCouponPercentage",
                table: "Bookings",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "Bookings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_AppliedCouponId",
                table: "Bookings",
                column: "AppliedCouponId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Coupons_AppliedCouponId",
                table: "Bookings",
                column: "AppliedCouponId",
                principalTable: "Coupons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Coupons_AppliedCouponId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_AppliedCouponId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AppliedCouponCode",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AppliedCouponId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AppliedCouponPercentage",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "Bookings");
        }
    }
}
