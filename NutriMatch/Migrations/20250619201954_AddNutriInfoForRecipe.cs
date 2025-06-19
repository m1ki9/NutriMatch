using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace NutriMatch.Migrations
{
    public partial class AddNutriInfoForRecipe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Calories",
                table: "Recipes",
                type: "real",
                nullable: false,
                defaultValue: 0f);
            migrationBuilder.AddColumn<float>(
                name: "Carbs",
                table: "Recipes",
                type: "real",
                nullable: false,
                defaultValue: 0f);
            migrationBuilder.AddColumn<float>(
                name: "Fat",
                table: "Recipes",
                type: "real",
                nullable: false,
                defaultValue: 0f);
            migrationBuilder.AddColumn<float>(
                name: "Protein",
                table: "Recipes",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Calories",
                table: "Recipes");
            migrationBuilder.DropColumn(
                name: "Carbs",
                table: "Recipes");
            migrationBuilder.DropColumn(
                name: "Fat",
                table: "Recipes");
            migrationBuilder.DropColumn(
                name: "Protein",
                table: "Recipes");
        }
    }
}
