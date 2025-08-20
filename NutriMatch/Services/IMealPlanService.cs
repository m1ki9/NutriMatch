using NutriMatch.Models;

namespace NutriMatch.Services
{
    public interface IMealPlanService
    {
        Task<MealPlanResult> GenerateWeeklyMealPlanAsync(string userId, MealPlanRequest request);
        Task<List<Recipe>> GetSuitableRecipesAsync(string mealType, DailyMacros targetMacros, List<string> dietaryRestrictions);
        Task<List<RestaurantMeal>> GetSuitableRestaurantMealsAsync(string mealType, DailyMacros targetMacros);
        Task<WeeklyMealPlan> GetMealPlanByIdAsync(int id, string userId);
        Task<List<WeeklyMealPlan>> GetUserMealPlansAsync(string userId);
        Task<bool> DeleteMealPlanAsync(int id, string userId);
    }
}