using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
#nullable disable
namespace NutriMatch.Migrations
{
    public partial class AddMealPlan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeeklyMealPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyMealPlans", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "MealSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Day = table.Column<string>(type: "text", nullable: false),
                    MealType = table.Column<string>(type: "text", nullable: false),
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    RestaurantMealId = table.Column<int>(type: "integer", nullable: false),
                    IsRestaurantMeal = table.Column<bool>(type: "boolean", nullable: false),
                    WeeklyMealPlanId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealSlots_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MealSlots_RestaurantMeals_RestaurantMealId",
                        column: x => x.RestaurantMealId,
                        principalTable: "RestaurantMeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MealSlots_WeeklyMealPlans_WeeklyMealPlanId",
                        column: x => x.WeeklyMealPlanId,
                        principalTable: "WeeklyMealPlans",
                        principalColumn: "Id");
                });
            migrationBuilder.CreateIndex(
                name: "IX_MealSlots_RecipeId",
                table: "MealSlots",
                column: "RecipeId");
            migrationBuilder.CreateIndex(
                name: "IX_MealSlots_RestaurantMealId",
                table: "MealSlots",
                column: "RestaurantMealId");
            migrationBuilder.CreateIndex(
                name: "IX_MealSlots_WeeklyMealPlanId",
                table: "MealSlots",
                column: "WeeklyMealPlanId");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MealSlots");
            migrationBuilder.DropTable(
                name: "WeeklyMealPlans");
        }
    }
}
