using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
#nullable disable
namespace NutriMatch.Migrations
{
    public partial class AddIngredientTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "food_macronutrients");
        }
    }
}
