using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace NutriMatch.Migrations
{
    public partial class RecipeStatusAdminMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Recipes");
            migrationBuilder.AddColumn<string>(
                name: "AdminComment",
                table: "Recipes",
                type: "text",
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "RecipeStatus",
                table: "Recipes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminComment",
                table: "Recipes");
            migrationBuilder.DropColumn(
                name: "RecipeStatus",
                table: "Recipes");
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Recipes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
