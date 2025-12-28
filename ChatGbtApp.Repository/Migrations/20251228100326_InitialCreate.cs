using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatGbtApp.Repository.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Url2 = table.Column<string>(type: "TEXT", nullable: true),
                    JobTitle = table.Column<string>(type: "TEXT", nullable: true),
                    CompanyName = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    Remote = table.Column<string>(type: "TEXT", nullable: true),
                    IsDistributed = table.Column<int>(type: "INTEGER", nullable: true),
                    MacroserviceScore = table.Column<string>(type: "TEXT", nullable: true),
                    ContractType = table.Column<string>(type: "TEXT", nullable: true),
                    Seniority = table.Column<string>(type: "TEXT", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", nullable: true),
                    HourlyMin = table.Column<string>(type: "TEXT", nullable: true),
                    HourlyMax = table.Column<string>(type: "TEXT", nullable: true),
                    SalaryIsEstimated = table.Column<string>(type: "TEXT", nullable: true),
                    SalaryOriginalText = table.Column<string>(type: "TEXT", nullable: true),
                    DeliveryPressureScore = table.Column<string>(type: "TEXT", nullable: true),
                    TechKeywords = table.Column<string>(type: "TEXT", nullable: true),
                    Confidence = table.Column<string>(type: "TEXT", nullable: true),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Rejected = table.Column<bool>(type: "INTEGER", nullable: false),
                    Marked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Applied = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    JobDescription = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Url);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
