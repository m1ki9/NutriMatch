using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace NutriMatch.Migrations
{
    public partial class ChangeRecipeIngredient : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeIngredients_Recipes_RecipeId",
                table: "RecipeIngredients");
            migrationBuilder.AlterColumn<int>(
                name: "RecipeId",
                table: "RecipeIngredients",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
            migrationBuilder.AddColumn<int>(
                name: "IngredientId",
                table: "RecipeIngredients",
                type: "integer",
                nullable: false,
                defaultValue: 0);
            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IngredientId",
                table: "RecipeIngredients",
                column: "IngredientId");
            migrationBuilder.AddForeignKey(
                name: "FK_RecipeIngredients_Recipes_RecipeId",
                table: "RecipeIngredients",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_RecipeIngredients_food_macronutrients_IngredientId",
                table: "RecipeIngredients",
                column: "IngredientId",
                principalTable: "food_macronutrients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeIngredients_Recipes_RecipeId",
                table: "RecipeIngredients");
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeIngredients_food_macronutrients_IngredientId",
                table: "RecipeIngredients");
            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredients_IngredientId",
                table: "RecipeIngredients");
            migrationBuilder.DropColumn(
                name: "IngredientId",
                table: "RecipeIngredients");
            migrationBuilder.AlterColumn<int>(
                name: "RecipeId",
                table: "RecipeIngredients",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
            migrationBuilder.AddForeignKey(
                name: "FK_RecipeIngredients_Recipes_RecipeId",
                table: "RecipeIngredients",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id");
        }
    }
}
