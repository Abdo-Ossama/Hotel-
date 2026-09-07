using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hotel_Booking.Migrations
{
    /// <inheritdoc />
    public partial class AddPropProviderOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StripeEventId",
                table: "PaymentTransactions",
                newName: "ProviderEventId");

            migrationBuilder.RenameColumn(
                name: "StripeSessionId",
                table: "Payments",
                newName: "ProviderOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProviderEventId",
                table: "PaymentTransactions",
                newName: "StripeEventId");

            migrationBuilder.RenameColumn(
                name: "ProviderOrderId",
                table: "Payments",
                newName: "StripeSessionId");
        }
    }
}
