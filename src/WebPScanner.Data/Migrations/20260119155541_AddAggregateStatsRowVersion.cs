using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebPScanner.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAggregateStatsRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AggregateStats",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AggregateStats");
        }
    }
}
