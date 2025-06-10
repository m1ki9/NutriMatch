using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NutriMatch.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.CreateTable(
            //     name: "food_macronutrients",
            //     columns: table => new
            //     {
            //         id = table.Column<int>(type: "integer", nullable: false)
            //             .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //         food_name = table.Column<string>(type: "text", nullable: false),
            //         energy_kcal = table.Column<float>(type: "real", nullable: false),
            //         protein_g = table.Column<float>(type: "real", nullable: false),
            //         carbohydrates_g = table.Column<float>(type: "real", nullable: false),
            //         total_fat_g = table.Column<float>(type: "real", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_food_macronutrients", x => x.id);
            //     });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "food_macronutrients");
        }
    }
}
