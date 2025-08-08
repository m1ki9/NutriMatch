using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace NutriMatch.Migrations
{
    public partial class RecipeModelChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Recipes");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Rating",
                table: "Recipes",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
