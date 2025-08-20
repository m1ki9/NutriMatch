using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriMatch.Migrations
{
    /// <inheritdoc />
    public partial class IngredientApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasPendingIngredients",
                table: "Recipes",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Ingredients",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasPendingIngredients",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Ingredients");
        }
    }
}
