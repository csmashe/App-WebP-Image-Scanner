using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebPScanner.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalScanDurationTicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TotalScanDurationTicks",
                table: "AggregateStats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalScanDurationTicks",
                table: "AggregateStats");
        }
    }
}
