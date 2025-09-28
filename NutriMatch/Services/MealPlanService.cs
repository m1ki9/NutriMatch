using Microsoft.EntityFrameworkCore;
using NutriMatch.Data;
using NutriMatch.Models;

namespace NutriMatch.Services
{
    public class MealPlanService : IMealPlanService
    {
        private readonly AppDbContext _context;
        private readonly Random _random;
        private readonly Dictionary<string, float> _mealTypeDistribution;

        public MealPlanService(AppDbContext context)
        {
            _context = context;
            _random = new Random();

            _mealTypeDistribution = new Dictionary<string, float>
            {
                { "breakfast", 0.25f },
                { "lunch", 0.35f },
                { "dinner", 0.35f },
                { "snack", 0.05f }
            };
        }

        public async Task<MealPlanResult> GenerateWeeklyMealPlanAsync(string userId, MealPlanRequest request)
        {
            var result = new MealPlanResult { Success = false };

            try
            {
                var weeklyPlan = new WeeklyMealPlan
                {
                    UserId = userId,
                    GeneratedAt = DateTime.UtcNow
                };

                var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
                var mealTypes = new[] { "breakfast", "lunch", "dinner" };

                var restaurantMealSlots = DistributeRestaurantMeals(request.RestaurantMealsPerWeek, days, mealTypes);

                foreach (var day in days)
                {
                    var dailyMacros = new DailyMacros
                    {
                        Calories = request.DailyCalories,
                        Protein = request.DailyProtein,
                        Carbs = request.DailyCarbs,
                        Fat = request.DailyFat
                    };

                    foreach (var mealType in mealTypes)
                    {
                        var mealSlot = new MealSlot
                        {
                            Day = day,
                            MealType = mealType
                        };

                        var targetMacros = CalculateMealMacros(dailyMacros, mealType);
                        var isRestaurantMeal = restaurantMealSlots.Contains($"{day}_{mealType}");

                        if (isRestaurantMeal)
                        {
                            var restaurantMeal = await SelectRestaurantMealAsync(mealType, targetMacros);
                            if (restaurantMeal != null)
                            {
                                mealSlot.RestaurantMeal = restaurantMeal;
                                mealSlot.IsRestaurantMeal = true;
                            }
                            else
                            {
                                var recipe = await SelectRecipeAsync(mealType, targetMacros);
                                mealSlot.Recipe = recipe;
                                mealSlot.IsRestaurantMeal = false;
                            }
                        }
                        else
                        {
                            var recipe = await SelectRecipeAsync(mealType, targetMacros);
                            if (recipe != null)
                            {
                                mealSlot.Recipe = recipe;
                                mealSlot.IsRestaurantMeal = false;
                            }
                        }

                        weeklyPlan.MealSlots.Add(mealSlot);
                    }

                    var remainingCalories = CalculateRemainingCalories(weeklyPlan.MealSlots.Where(ms => ms.Day == day).ToList(), dailyMacros.Calories);
                    if (remainingCalories > 100)
                    {
                        var snackMacros = new DailyMacros
                        {
                            Calories = remainingCalories,
                            Protein = remainingCalories * 0.15f / 4,
                            Carbs = remainingCalories * 0.50f / 4,
                            Fat = remainingCalories * 0.35f / 9
                        };

                        var snackSlot = new MealSlot
                        {
                            Day = day,
                            MealType = "snack"
                        };

                        var snackRecipe = await SelectRecipeAsync("snack", snackMacros);
                        if (snackRecipe != null)
                        {
                            snackSlot.Recipe = snackRecipe;
                            snackSlot.IsRestaurantMeal = false;
                            weeklyPlan.MealSlots.Add(snackSlot);
                        }
                    }
                }

                _context.WeeklyMealPlans.Add(weeklyPlan);
                await _context.SaveChangesAsync();

                result.WeeklyMealPlan = weeklyPlan;
                result.DailyMacroTotals = CalculateDailyMacroTotals(weeklyPlan);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Failed to generate meal plan: {ex.Message}";
            }

            return result;
        }

        public async Task<bool> DeleteMealPlanAsync(int id, string userId)
        {
            try
            {
                var mealPlan = await _context.WeeklyMealPlans
                    .Include(wmp => wmp.MealSlots)
                    .FirstOrDefaultAsync(wmp => wmp.Id == id && wmp.UserId == userId);

                if (mealPlan == null)
                {
                    return false;
                }

                _context.MealSlots.RemoveRange(mealPlan.MealSlots);

                _context.WeeklyMealPlans.Remove(mealPlan);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private HashSet<string> DistributeRestaurantMeals(int totalRestaurantMeals, string[] days, string[] mealTypes)
        {
            var restaurantSlots = new HashSet<string>();
            var availableSlots = new List<string>();

            foreach (var day in days)
            {
                foreach (var mealType in mealTypes)
                {
                    availableSlots.Add($"{day}_{mealType}");
                }
            }

            for (int i = 0; i < Math.Min(totalRestaurantMeals, availableSlots.Count); i++)
            {
                if (availableSlots.Count > 0)
                {
                    var randomIndex = _random.Next(availableSlots.Count);
                    var selectedSlot = availableSlots[randomIndex];
                    restaurantSlots.Add(selectedSlot);
                    availableSlots.RemoveAt(randomIndex);
                }
            }

            return restaurantSlots;
        }

        private DailyMacros CalculateMealMacros(DailyMacros dailyMacros, string mealType)
        {
            var distribution = _mealTypeDistribution.GetValueOrDefault(mealType, 0.25f);

            return new DailyMacros
            {
                Calories = dailyMacros.Calories * distribution,
                Protein = dailyMacros.Protein * distribution,
                Carbs = dailyMacros.Carbs * distribution,
                Fat = dailyMacros.Fat * distribution
            };
        }

        private async Task<Recipe> SelectRecipeAsync(string mealType, DailyMacros targetMacros)
        {
            var query = _context.Recipes
                .Include(r => r.RecipeIngredients)
                .Where(r => r.RecipeStatus == "Accepted");

            if (!string.IsNullOrEmpty(mealType))
            {
                query = query.Where(r => r.Type.Contains(mealType));
            }

            var recipes = await query.ToListAsync();

            if (!recipes.Any())
            {
                recipes = await _context.Recipes
                    .Where(r => r.RecipeStatus == "Accepted")
                    .ToListAsync();
            }

            if (!recipes.Any()) return null;

            var scoredRecipes = recipes.Select(recipe => new
            {
                Recipe = recipe,
                Score = CalculateMacroMatchScore(recipe, targetMacros)
            })
            .OrderByDescending(x => x.Score)
            .Take(10)
            .ToList();

            var selectedRecipe = scoredRecipes[_random.Next(Math.Min(3, scoredRecipes.Count))].Recipe;

            return selectedRecipe;
        }

        private async Task<RestaurantMeal> SelectRestaurantMealAsync(string mealType, DailyMacros targetMacros)
        {
            var query = _context.RestaurantMeals.AsQueryable();

            if (!string.IsNullOrEmpty(mealType))
            {
                query = query.Where(rm => rm.Type.Contains(mealType));
            }

            var restaurantMeals = await query.ToListAsync();

            if (!restaurantMeals.Any()) return null;

            var scoredMeals = restaurantMeals.Select(meal => new
            {
                Meal = meal,
                Score = CalculateMacroMatchScore(meal, targetMacros)
            })
            .OrderByDescending(x => x.Score)
            .Take(5)
            .ToList();

            return scoredMeals[_random.Next(scoredMeals.Count)].Meal;
        }

        private double CalculateMacroMatchScore(Recipe recipe, DailyMacros targetMacros)
        {
            var calorieMatch = 1.0 - Math.Abs(recipe.Calories - targetMacros.Calories) / targetMacros.Calories;
            var proteinMatch = 1.0 - Math.Abs(recipe.Protein - targetMacros.Protein) / Math.Max(targetMacros.Protein, 1);
            var carbMatch = 1.0 - Math.Abs(recipe.Carbs - targetMacros.Carbs) / Math.Max(targetMacros.Carbs, 1);
            var fatMatch = 1.0 - Math.Abs(recipe.Fat - targetMacros.Fat) / Math.Max(targetMacros.Fat, 1);

            return (calorieMatch * 0.4 + proteinMatch * 0.2 + carbMatch * 0.2 + fatMatch * 0.2) * 100;
        }

        private double CalculateMacroMatchScore(RestaurantMeal meal, DailyMacros targetMacros)
        {
            var calorieMatch = 1.0 - Math.Abs(meal.Calories - targetMacros.Calories) / targetMacros.Calories;
            var proteinMatch = 1.0 - Math.Abs(meal.Protein - targetMacros.Protein) / Math.Max(targetMacros.Protein, 1);
            var carbMatch = 1.0 - Math.Abs(meal.Carbs - targetMacros.Carbs) / Math.Max(targetMacros.Carbs, 1);
            var fatMatch = 1.0 - Math.Abs(meal.Fat - targetMacros.Fat) / Math.Max(targetMacros.Fat, 1);

            return (calorieMatch * 0.4 + proteinMatch * 0.2 + carbMatch * 0.2 + fatMatch * 0.2) * 100;
        }

        private float CalculateRemainingCalories(List<MealSlot> dayMeals, float targetCalories)
        {
            var totalCalories = dayMeals.Sum(ms =>
                ms.IsRestaurantMeal ? (ms.RestaurantMeal?.Calories ?? 0) : (ms.Recipe?.Calories ?? 0));

            return Math.Max(0, targetCalories - totalCalories);
        }

        private Dictionary<string, DailyMacros> CalculateDailyMacroTotals(WeeklyMealPlan weeklyPlan)
        {
            var dailyTotals = new Dictionary<string, DailyMacros>();
            var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

            foreach (var day in days)
            {
                var dayMeals = weeklyPlan.MealSlots.Where(ms => ms.Day == day).ToList();

                dailyTotals[day] = new DailyMacros
                {
                    Calories = dayMeals.Sum(ms => ms.IsRestaurantMeal ? (ms.RestaurantMeal?.Calories ?? 0) : (ms.Recipe?.Calories ?? 0)),
                    Protein = dayMeals.Sum(ms => ms.IsRestaurantMeal ? (ms.RestaurantMeal?.Protein ?? 0) : (ms.Recipe?.Protein ?? 0)),
                    Carbs = dayMeals.Sum(ms => ms.IsRestaurantMeal ? (ms.RestaurantMeal?.Carbs ?? 0) : (ms.Recipe?.Carbs ?? 0)),
                    Fat = dayMeals.Sum(ms => ms.IsRestaurantMeal ? (ms.RestaurantMeal?.Fat ?? 0) : (ms.Recipe?.Fat ?? 0))
                };
            }

            return dailyTotals;
        }

        public async Task<WeeklyMealPlan> GetMealPlanByIdAsync(int id, string userId)
        {
#pragma warning disable CS8603 
            return await _context.WeeklyMealPlans
                .Include(wmp => wmp.MealSlots)
                    .ThenInclude(ms => ms.Recipe)
                        .ThenInclude(r => r.RecipeIngredients)
                .Include(wmp => wmp.MealSlots)
                    .ThenInclude(ms => ms.RestaurantMeal)
                        .ThenInclude(rm => rm.Restaurant)
                .FirstOrDefaultAsync(wmp => wmp.Id == id && wmp.UserId == userId);
        }

        public async Task<List<WeeklyMealPlan>> GetUserMealPlansAsync(string userId)
        {
            return await _context.WeeklyMealPlans
                .Where(wmp => wmp.UserId == userId)
                .Include(wmp => wmp.MealSlots)
                .OrderByDescending(wmp => wmp.GeneratedAt)
                .ToListAsync();
        }
    }
}