using Microsoft.EntityFrameworkCore;
using NutriMatch.Models;
namespace NutriMatch.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Ingredient>()
                .ToTable("food_macronutrients");
            modelBuilder.Entity<Ingredient>()
                .Property(e => e.Id)
                .HasColumnName("id");
            modelBuilder.Entity<Ingredient>()
                .Property(e => e.Name)
                .HasColumnName("food_name");
            modelBuilder.Entity<Ingredient>()
                .Property(e => e.Calories)
                .HasColumnName("energy_kcal");
            modelBuilder.Entity<Ingredient>()
                .Property(e => e.Protein)
                .HasColumnName("protein_g");
            modelBuilder.Entity<Ingredient>()
                .Property(e => e.Fat)
                .HasColumnName("total_fat_g");
            modelBuilder.Entity<Ingredient>()
                .Property(e => e.Carbs)
                .HasColumnName("carbohydrates_g");    
        }
    }
}