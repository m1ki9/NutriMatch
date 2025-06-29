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
        
        public DbSet<RestaurantMeal> RestaurantMeals { get; set; }
        
        public DbSet<Restaurant> Restaurants { get; set; }

       
    }
}