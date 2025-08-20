using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace NutriMatch.Migrations
{
    public partial class MealPlanFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealSlots_Recipes_RecipeId",
                table: "MealSlots");
            migrationBuilder.DropForeignKey(
                name: "FK_MealSlots_RestaurantMeals_RestaurantMealId",
                table: "MealSlots");
            migrationBuilder.AlterColumn<int>(
                name: "RestaurantMealId",
                table: "MealSlots",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
            migrationBuilder.AlterColumn<int>(
                name: "RecipeId",
                table: "MealSlots",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
            migrationBuilder.AddForeignKey(
                name: "FK_MealSlots_Recipes_RecipeId",
                table: "MealSlots",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id");
            migrationBuilder.AddForeignKey(
                name: "FK_MealSlots_RestaurantMeals_RestaurantMealId",
                table: "MealSlots",
                column: "RestaurantMealId",
                principalTable: "RestaurantMeals",
                principalColumn: "Id");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealSlots_Recipes_RecipeId",
                table: "MealSlots");
            migrationBuilder.DropForeignKey(
                name: "FK_MealSlots_RestaurantMeals_RestaurantMealId",
                table: "MealSlots");
            migrationBuilder.AlterColumn<int>(
                name: "RestaurantMealId",
                table: "MealSlots",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
            migrationBuilder.AlterColumn<int>(
                name: "RecipeId",
                table: "MealSlots",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
            migrationBuilder.AddForeignKey(
                name: "FK_MealSlots_Recipes_RecipeId",
                table: "MealSlots",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_MealSlots_RestaurantMeals_RestaurantMealId",
                table: "MealSlots",
                column: "RestaurantMealId",
                principalTable: "RestaurantMeals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
