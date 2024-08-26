using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBot.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswersJsonColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Answers",
                table: "Tests",
                newName: "AnswersJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AnswersJson",
                table: "Tests",
                newName: "Answers");
        }
    }
}
