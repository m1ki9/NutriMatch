using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace NutriMatch.Migrations
{
    public partial class AddRestaurantMealRelationship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "RestaurantMeals",
                type: "integer",
                nullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_RestaurantMeals_RestaurantId",
                table: "RestaurantMeals",
                column: "RestaurantId");
            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantMeals_Restaurants_RestaurantId",
                table: "RestaurantMeals",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantMeals_Restaurants_RestaurantId",
                table: "RestaurantMeals");
            migrationBuilder.DropIndex(
                name: "IX_RestaurantMeals_RestaurantId",
                table: "RestaurantMeals");
            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "RestaurantMeals");
        }
    }
}
