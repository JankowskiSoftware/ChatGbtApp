using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatGbtApp.Repository.Migrations
{
    /// <inheritdoc />
    public partial class Added_MaxPLNRate_and_PLNRemote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlRemote",
                table: "Jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyMaxPLN",
                table: "Jobs",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlRemote",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "HourlyMaxPLN",
                table: "Jobs");
        }
    }
}
