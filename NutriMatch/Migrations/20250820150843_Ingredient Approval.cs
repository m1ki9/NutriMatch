using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace NutriMatch.Migrations
{
    public partial class IngredientApproval : Migration
    {
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
