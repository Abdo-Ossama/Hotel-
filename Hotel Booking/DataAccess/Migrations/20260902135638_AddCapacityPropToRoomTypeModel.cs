using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hotel_Booking.Migrations
{
    /// <inheritdoc />
    public partial class AddCapacityPropToRoomTypeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "RoomTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
        UPDATE RoomTypes
        SET Capacity = CASE 
            WHEN Name = 'Single' THEN 1
            WHEN Name = 'Double' THEN 2
            WHEN Name = 'Suite'  THEN 3
            ELSE 1
        END
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "RoomTypes");
        }
    }
}
