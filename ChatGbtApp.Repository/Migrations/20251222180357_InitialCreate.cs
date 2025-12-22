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
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Company = table.Column<string>(type: "TEXT", nullable: true),
                    JobTitle = table.Column<string>(type: "TEXT", nullable: true),
                    MatchScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Remote = table.Column<string>(type: "TEXT", nullable: true),
                    Frontend = table.Column<string>(type: "TEXT", nullable: true),
                    DotNetRole = table.Column<string>(type: "TEXT", nullable: true),
                    SeniorityFit = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    Recommendation = table.Column<string>(type: "TEXT", nullable: true),
                    MissingSkills = table.Column<string>(type: "TEXT", nullable: false),
                    MissingAtsKeywoards = table.Column<string>(type: "TEXT", nullable: false),
                    Strengths = table.Column<string>(type: "TEXT", nullable: false),
                    JobDescription = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    Marked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Applied = table.Column<bool>(type: "INTEGER", nullable: false)
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
