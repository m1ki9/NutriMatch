using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NutriMatch.Data;
using System.Text.Json;
using NutriMatch.Models;
namespace NutriMatch.Controllers
{
    public class RestaurantsController : Controller
    {
        private readonly AppDbContext _context;
        public RestaurantsController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var restaurants = await _context.Restaurants.ToListAsync();
            return View(restaurants);
        }
        public async Task<IActionResult> GetRestaurantMeals(int? id,int? minCalories, int? maxCalories, int? minProtein, int? maxProtein, int? minCarbs, int? maxCarbs, int? minFat, int? maxFat)
        {
            if (id == null)
            {
                return NotFound();
            }
            var restaurant = await _context.Restaurants.Include(r => r.RestaurantMeals)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (restaurant == null)
            {
                return NotFound();
            }
            var filteredMeals = restaurant.RestaurantMeals
        .Where(r =>
            (minCalories == null || r.Calories >= minCalories) &&
            (maxCalories == null || r.Calories <= maxCalories) &&
            (minProtein == null || r.Protein >= minProtein) &&
            (maxProtein == null || r.Protein <= maxProtein) &&
            (minFat == null || r.Fat >= minFat) &&
            (maxFat == null || r.Fat <= maxFat) &&
            (minCarbs == null || r.Carbs >= minCarbs) &&
            (maxCarbs == null || r.Carbs <= maxCarbs)
        )
        .ToList();
            Console.WriteLine($"Total meals for restaurant {id}: {filteredMeals.Count}");
            return PartialView("_RestaurantMealsPartial", filteredMeals);
        }
        public async Task ClassifyAllRestaurantMeals()
        {
            var meals = await GetUnclassifiedMealsFromDatabase();
            foreach (var meal in meals)
            {
                var mealTypes = GenerateMealTypes(meal);
                await UpdateMealTypesInDatabase(meal.Id, mealTypes);
            }
        }
        private MealKeywords LoadKeywordsFromJson()
        {
            var filePath = "Data/meal_keywords.json";
            if (!System.IO.File.Exists(filePath))
            {
                return new MealKeywords 
                { 
                    Breakfast = new List<string>(), 
                    Main = new List<string>(), 
                    Snack = new List<string>() 
                };
            }
            var jsonString = System.IO.File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<MealKeywords>(jsonString, options) ?? new MealKeywords 
            { 
                Breakfast = new List<string>(), 
                Main = new List<string>(), 
                Snack = new List<string>() 
            };
        }
    public List<string> GenerateMealTypes(RestaurantMeal meal)
    {
        if (meal.Calories == 0 || 
            (!string.IsNullOrEmpty(meal.ItemDescription) && 
             (meal.ItemDescription.ToLower().Contains("wine") || 
              meal.ItemDescription.ToLower().Contains("beer") || 
              meal.ItemDescription.ToLower().Contains("spirits") ||   
              meal.ItemDescription.ToLower().Contains("beverages")
              )))
        {
            return new List<string> { "drink" };
        }
        var keywords = LoadKeywordsFromJson();
        var tags = new HashSet<string>();
        string NormalizeWord(string word)
        {
            word = word.ToLower().Trim();
            if (word.EndsWith("ies") && word.Length > 4)
                return word.Substring(0, word.Length - 3) + "y";
            if (word.EndsWith("es") && word.Length > 3)
                return word.Substring(0, word.Length - 2);
            if (word.EndsWith("s") && word.Length > 3 && !word.EndsWith("ss"))
                return word.Substring(0, word.Length - 1);
            return word;
        }
        int CountKeywordMatches(IEnumerable<string> words, HashSet<string> keywords, bool isTitle = false)
        {
            int count = 0;
            foreach (var word in words)
            {
                bool matches = keywords.Contains(word) || keywords.Contains(NormalizeWord(word));
                if (matches)
                    count += isTitle ? 3 : 1;
            }
            return count;
        }
        var breakfastKeywords = new HashSet<string>(keywords.Breakfast ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        var mainKeywords = new HashSet<string>(keywords.Main ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        var snackKeywords = new HashSet<string>(keywords.Snack ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        var titleWords = meal.ItemName.ToLower()
            .Split(new char[] { ' ', '-', '_', ',', '.', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
        var descriptionWords = new HashSet<string>();
        if (!string.IsNullOrEmpty(meal.ItemDescription))
        {
            var words = meal.ItemDescription.ToLower()
                .Split(new char[] { ' ', '-', '_', ',', '.', '(', ')', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var w in words) descriptionWords.Add(w);
        }
        var allWords = titleWords.Concat(descriptionWords).ToList();
        int breakfastScore = CountKeywordMatches(titleWords, breakfastKeywords, true) +
                            CountKeywordMatches(descriptionWords, breakfastKeywords, false);
        int mainScore = CountKeywordMatches(titleWords, mainKeywords, true) +
                        CountKeywordMatches(descriptionWords, mainKeywords, false);
        int snackScore = CountKeywordMatches(titleWords, snackKeywords, true) +
                        CountKeywordMatches(descriptionWords, snackKeywords, false);
        int lunchScore = mainScore;
        int dinnerScore = mainScore;
            float calories = meal.Calories;
            float proteinRatio = (meal.Protein * 4) / calories * 100;
            float carbRatio = (meal.Carbs * 4) / calories * 100;
            float fatRatio = (meal.Fat * 9) / calories * 100;
            if (calories < 250)
            {
                snackScore += 2;
                breakfastScore += 1;
                dinnerScore -= 2;
                lunchScore -= 2;
            }
            else if (calories <= 500)
            {
                lunchScore += 1;
                dinnerScore += 1;
                breakfastScore += 2;
            }
            else
            {
                dinnerScore += 2;
                lunchScore += 2;
                breakfastScore -= 1;
                snackScore -= 2;
            }
            if (proteinRatio >= 25)
            {
                dinnerScore += 2;
                lunchScore += 2;
            }
            else if (carbRatio >= 50)
            {
                breakfastScore += 1;
                snackScore += 1;
            }
            if (fatRatio > 30)
            {
                dinnerScore += 1;
                snackScore += 1;
            }
        var results = new List<(string tag, int score)>
        {
            ("breakfast", breakfastScore),
            ("lunch", lunchScore),
            ("dinner", dinnerScore),
            ("snack", snackScore)
        }.OrderByDescending(x => x.score).ToList();
        tags.Add(results[0].tag);
        for (int i = 1; i < results.Count; i++)
        {
            if (results[i].score > 0 && results[i].score >= results[0].score * 0.6)
                tags.Add(results[i].tag);
        }
        return tags.ToList();
    }
    private async Task<List<RestaurantMeal>> GetUnclassifiedMealsFromDatabase()
    {
        return await _context.RestaurantMeals.ToListAsync();
    }
    private async Task UpdateMealTypesInDatabase(int mealId, List<string> mealTypes)
    {
        var meal = await _context.RestaurantMeals.FindAsync(mealId);
        meal.Type = mealTypes;
        await _context.SaveChangesAsync();
    }
    }
}