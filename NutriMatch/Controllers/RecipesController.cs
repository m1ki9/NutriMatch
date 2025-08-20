using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NutriMatch.Data;
using NutriMatch.Models;
using System.Text.Json;
using Microsoft.Identity.Client;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.Intrinsics.X86;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNet.Identity;



namespace NutriMatch.Controllers
{

    public class MealKeywords
    {
        public List<string> Breakfast { get; set; }
        public List<string> Main { get; set; }
        public List<string> Snack { get; set; }
    }


    public class RecipesController : Controller
    {




        // Load keywords from JSON file
        private MealKeywords LoadKeywordsFromJson()
        {
            var filePath = "Data/meal_keywords.json";
            if (!System.IO.File.Exists(filePath))
            {
                // Return empty keywords if file doesn't exist
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

        // Generate tags method - now loads keywords from JSON automatically
        public List<string> GenerateRecipeTags(Recipe recipe, List<SelectedIngredient> ingredients)
        {
            // Load keywords from JSON file
            var keywords = LoadKeywordsFromJson();

            var tags = new HashSet<string>();

            // Normalize words (handle plural/singular)
            string NormalizeWord(string word)
            {
                word = word.ToLower().Trim();
                if (word.EndsWith("ies") && word.Length > 4)
                    return word.Substring(0, word.Length - 3) + "y"; // berries -> berry
                if (word.EndsWith("es") && word.Length > 3)
                    return word.Substring(0, word.Length - 2); // tomatoes -> tomato
                if (word.EndsWith("s") && word.Length > 3 && !word.EndsWith("ss"))
                    return word.Substring(0, word.Length - 1); // eggs -> egg
                return word;
            }

            // Count keyword matches with normalization
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

            // Check if words contain any keyword (handles plural/singular)
            bool ContainsKeyword(IEnumerable<string> words, HashSet<string> keywords)
            {
                return words.Any(word =>
                    keywords.Contains(word) ||
                    keywords.Contains(NormalizeWord(word)) ||
                    keywords.Any(k => NormalizeWord(k) == NormalizeWord(word))
                );
            }

            // Convert keyword lists to HashSets for efficient lookup
            var breakfastKeywords = new HashSet<string>(keywords.Breakfast ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            var mainKeywords = new HashSet<string>(keywords.Main ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            var snackKeywords = new HashSet<string>(keywords.Snack ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

            // Tokenize title
            var titleWords = recipe.Title.ToLower()
                .Split(new char[] { ' ', '-', '_', ',', '.', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

            // Tokenize ingredients
            var ingredientWords = new HashSet<string>();
            foreach (var ing in ingredients)
            {
                var words = ing.Name.ToLower()
                    .Split(new char[] { ' ', '-', '_', ',', '.', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var w in words) ingredientWords.Add(w);
            }

            var allWords = titleWords.Concat(ingredientWords).ToList();

            // Initial keyword scores
            int breakfastScore = CountKeywordMatches(titleWords, breakfastKeywords, true) +
                                 CountKeywordMatches(ingredientWords, breakfastKeywords, false);

            int mainScore = CountKeywordMatches(titleWords, mainKeywords, true) +
                            CountKeywordMatches(ingredientWords, mainKeywords, false);

            int snackScore = CountKeywordMatches(titleWords, snackKeywords, true) +
                             CountKeywordMatches(ingredientWords, snackKeywords, false);

            int lunchScore = mainScore;
            int dinnerScore = mainScore;

            // Macro analysis
            float calories = Math.Max(recipe.Calories, 1);
            float proteinRatio = (recipe.Protein * 4) / calories * 100;
            float carbRatio = (recipe.Carbs * 4) / calories * 100;
            float fatRatio = (recipe.Fat * 9) / calories * 100;

            // Calorie-based adjustments
            if (calories < 150) // Very light snacks
            {
                snackScore += 5;
                breakfastScore -= 2;
                lunchScore -= 3;
                dinnerScore -= 4;
            }
            else if (calories < 300) // Light snacks or small breakfast
            {
                snackScore += 3;
                breakfastScore += 2;
                lunchScore -= 1;
                dinnerScore -= 2;
            }
            else if (calories < 450) // Breakfast or light lunch
            {
                breakfastScore += 3;
                lunchScore += 2;
                snackScore -= 1;
                dinnerScore -= 1;
            }
            else if (calories < 650) // Standard lunch/dinner
            {
                lunchScore += 3;
                dinnerScore += 2;
                breakfastScore -= 1;
                snackScore -= 3;
            }
            else // Heavy meals - likely dinner
            {
                dinnerScore += 4;
                lunchScore += 1;
                breakfastScore -= 3;
                snackScore -= 4;
            }

            // Protein content analysis
            if (proteinRatio > 30) // Very high protein - likely main meals
            {
                dinnerScore += 3;
                lunchScore += 2;
                breakfastScore += 1;
                snackScore -= 1;
            }
            else if (proteinRatio > 20) // High protein
            {
                dinnerScore += 2;
                lunchScore += 1;
            }
            else if (proteinRatio < 10) // Low protein - often snacks/sides
            {
                snackScore += 2;
                dinnerScore -= 1;
                lunchScore -= 1;
            }

            // Carbohydrate analysis
            if (carbRatio > 60) // Very high carbs - breakfast cereals, snacks
            {
                breakfastScore += 2;
                snackScore += 2;
                dinnerScore -= 1;
            }
            else if (carbRatio < 20) // Very low carbs - likely protein-heavy meals
            {
                dinnerScore += 1;
                lunchScore += 1;
            }

            // Fat content analysis
            if (fatRatio > 40) // High fat - rich dinner dishes or snacks
            {
                dinnerScore += 2;
                snackScore += 1;
                breakfastScore -= 1;
            }

            // Compose and sort results
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








        float ConvertType(float number, string unit)
        {
            float result;
            switch (unit.ToLower())
            {
                case "g":
                case "ml":
                    result = number / 100;
                    break;
                case "oz":
                    result = (float)(number * 28.3495 / 100);
                    break;
                case "tbsp":
                    result = (float)(number * 15 / 100);
                    break;
                case "tsp":
                    result = (float)(number * 5 / 100);
                    break;
                case "cup":
                    result = (float)(number * 240 / 100);
                    break;
                default:
                    return 0;
            }

            return result;
        }

        private readonly AppDbContext _context;
        private readonly ILogger<RecipesController> _logger;

        public RecipesController(AppDbContext context, ILogger<RecipesController> logger)
        {
            _context = context;
            _logger = logger;
        }


        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get all recipes with user information
            var recipes = await _context.Recipes
                .Where(r => r.RecipeStatus == "Accepted")
                .Include(r => r.User)
                .Include(r => r.Ratings)
                .ToListAsync();


            foreach (var recipe in recipes)
            {
                recipe.Rating = recipe.Ratings.Any() ? recipe.Ratings.Average(r => r.Rating) : 0;
            }
            // If user is authenticated, get their favorite recipe IDs
            List<int> favoriteRecipeIds = new List<int>();
            if (!string.IsNullOrEmpty(userId))
            {
                favoriteRecipeIds = await _context.FavoriteRecipes
                    .Where(fr => fr.UserId == userId)
                    .Select(fr => fr.RecipeId)
                    .ToListAsync();
            }

            // Pass favorite IDs to the view
            ViewBag.FavoriteRecipeIds = favoriteRecipeIds;
            ViewBag.userId = userId;

            return View(recipes);
        }

        [Route("Recipes/Details/{id}")]
        public async Task<IActionResult> Details(int? id, bool isOwner = false, String recipeDetailsDisplayContorol = "")
        {
            // 1. NULL CHECK - Ensure ID was provided
            if (id == null)
            {
                return NotFound();
            }

            // 2. FETCH THE RECIPE WITH RELATED DATA
            // Include() eagerly loads related data to avoid lazy loading issues
            // ThenInclude() allows you to include nested related data
            var recipe = await _context.Recipes.Include(r => r.User)
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(m => m.Id == id);

            // 3. RECIPE EXISTS CHECK
            if (recipe == null)
            {
                return NotFound();
            }

            if (recipeDetailsDisplayContorol == "Declined")
            {
                return PartialView("_RecipeDeclinePartial", recipe);
            }
            else
            {


                // 4. GET CURRENT USER ID FOR RATING FUNCTIONALITY
                // This gets the authenticated user's ID from the claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // 5. DETERMINE OWNERSHIP
                // Check if the current user is the owner of this recipe
                // This will override the route parameter if user is authenticated
                bool actualIsOwner = !string.IsNullOrEmpty(userId) && recipe.UserId == userId;

                // 6. GET RATING DATA FOR THIS RECIPE
                // Call helper method to get all rating-related information
                var (averageRating, totalRatings, userRating, hasUserRated) =
                    await GetRatingDataAsync(id.Value, userId);

                // 7. CHECK IF RECIPE IS FAVORITED BY CURRENT USER
                bool isFavorited = false;
                if (!string.IsNullOrEmpty(userId))
                {
                    isFavorited = await _context.FavoriteRecipes
                        .AnyAsync(fr => fr.UserId == userId && fr.RecipeId == id.Value);
                }

                if (recipeDetailsDisplayContorol == "Buttons")
                {
                    ViewBag.AddAdminButtons = true;
                }
                // 8. PASS DATA TO VIEW VIA VIEWBAG
                // These values will be available in your Razor view
                ViewBag.IsOwner = actualIsOwner;          // Whether current user owns this recipe
                ViewBag.AverageRating = averageRating;    // Calculated average rating (0.0 to 5.0)
                ViewBag.TotalRatings = totalRatings;      // Total number of ratings
                ViewBag.UserRating = userRating;          // Current user's rating (0 if not rated)
                ViewBag.HasUserRated = hasUserRated;      // Boolean - has current user rated this recipe
                ViewBag.IsFavorited = isFavorited;        // Boolean - is this recipe favorited by current user

                // 9. RETURN PARTIAL VIEW
                // Returns the partial view with the recipe model and ViewBag data
                return PartialView("_RecipeDetailsPartial", recipe);
            }
        }

        [Authorize]
        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Instructions")] Recipe recipe)
        {



            if (ModelState.IsValid)
            {

                var file = Request.Form.Files.GetFile("RecipeImage");
                if (file != null && file.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    recipe.ImageUrl = "/images/" + uniqueFileName;
                }
                else
                {
                    Console.WriteLine("No file uploaded or file is empty.");
                }

                recipe.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                recipe.Type = new List<string> { " " };
                _context.Add(recipe);
                await _context.SaveChangesAsync();
                string selectedIngredients = Request.Form["Ingredients"];
                List<SelectedIngredient> ingredients = JsonSerializer.Deserialize<List<SelectedIngredient>>(selectedIngredients);
                float totalCalories = 0;
                float totalProtein = 0;
                float totalCarbs = 0;
                float totalFat = 0;



                foreach (var i in ingredients)
                {
                    _context.RecipeIngredients.Add(new RecipeIngredient
                    {
                        RecipeId = recipe.Id,
                        IngredientId = i.Id,
                        Unit = i.Unit,
                        Quantity = i.Quantity
                    });

                    Ingredient tempIngredient = _context.Ingredients.Find(i.Id);



                    totalCalories += ConvertType(tempIngredient.Calories, i.Unit) * i.Quantity;
                    totalProtein += ConvertType(tempIngredient.Protein, i.Unit) * i.Quantity;
                    totalCarbs += ConvertType(tempIngredient.Carbs, i.Unit) * i.Quantity;
                    totalFat += ConvertType(tempIngredient.Fat, i.Unit) * i.Quantity;
                }

                recipe.Calories = MathF.Round(totalCalories, MidpointRounding.AwayFromZero);
                recipe.Protein = MathF.Round(totalProtein, MidpointRounding.AwayFromZero);
                recipe.Carbs = MathF.Round(totalCarbs, MidpointRounding.AwayFromZero);
                recipe.Fat = MathF.Round(totalFat, MidpointRounding.AwayFromZero);



                recipe.Type = GenerateRecipeTags(recipe, ingredients);

                _context.Update(recipe);


                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));



            }
            else
            {
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"Key: {key} - Error: {error.ErrorMessage}");
                    }
                }


                Console.WriteLine("Model state is invalid. Please check the input data.");
            }
            return View(recipe);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, bool requiresChange = false)
        {

            if (requiresChange)
            {
                ViewBag.RequireChange = true;
            }
            else
            {
                ViewBag.RequireChange = false;
            }

            if (id == null)
            {
                return NotFound();
            }



            var recipe = await _context.Recipes
        .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
        .FirstOrDefaultAsync(r => r.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }

            if (recipe.UserId != User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }

            return View(recipe);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,Title,Instructions")] Recipe recipe)
        {


            if (ModelState.IsValid)
            {
                var file = Request.Form.Files.GetFile("RecipeImage");
                if (file != null && file.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    recipe.ImageUrl = "/images/" + uniqueFileName;
                }
                else
                {

                    var existing = Request.Form["ExistingImageUrl"].ToString();
                    if (existing != null)
                    {
                        recipe.ImageUrl = existing;
                    }
                }

                await _context.RecipeIngredients.Where(ri => ri.RecipeId == recipe.Id).ExecuteDeleteAsync();

                string selectedIngredients = Request.Form["Ingredients"];
                List<SelectedIngredient> ingredients = JsonSerializer.Deserialize<List<SelectedIngredient>>(selectedIngredients);
                float totalCalories = 0;
                float totalProtein = 0;
                float totalCarbs = 0;
                float totalFat = 0;

                foreach (var i in ingredients)
                {
                    _context.RecipeIngredients.Add(new RecipeIngredient
                    {
                        RecipeId = recipe.Id,
                        IngredientId = i.Id,
                        Unit = i.Unit,
                        Quantity = i.Quantity
                    });

                    Ingredient tempIngredient = _context.Ingredients.Find(i.Id);

                    totalCalories += ConvertType(tempIngredient.Calories, i.Unit) * i.Quantity;
                    totalProtein += ConvertType(tempIngredient.Protein, i.Unit) * i.Quantity;
                    totalCarbs += ConvertType(tempIngredient.Carbs, i.Unit) * i.Quantity;
                    totalFat += ConvertType(tempIngredient.Fat, i.Unit) * i.Quantity;
                }

                recipe.Calories = MathF.Round(totalCalories, MidpointRounding.AwayFromZero);
                recipe.Protein = MathF.Round(totalProtein, MidpointRounding.AwayFromZero);
                recipe.Carbs = MathF.Round(totalCarbs, MidpointRounding.AwayFromZero);
                recipe.Fat = MathF.Round(totalFat, MidpointRounding.AwayFromZero);
                recipe.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                recipe.Type = GenerateRecipeTags(recipe, ingredients);

                _context.Update(recipe);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(MyRecipes));
            }
            else
            {
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"Key: {key} - Error: {error.ErrorMessage}");
                    }
                }

                Console.WriteLine("Model state is invalid. Please check the input data.");
            }

            return View(recipe);
        }



        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipes.Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }

            return PartialView("_RecipeDeletePartial", recipe);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe != null)
            {
                _context.Recipes.Remove(recipe);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecipeExists(int id)
        {
            return _context.Recipes.Any(e => e.Id == id);
        }


        public async Task<ActionResult<List<Ingredient>>> getSuggestions([FromQuery] String query)
        {
            List<Ingredient> suggestions = await _context.Ingredients
            .Where(i => EF.Functions.ILike(i.Name, $"%{query}%"))
            .OrderBy(i => i.Name)
            .Take(5)
            .ToListAsync();

            return suggestions;
        }


        public ActionResult MyRecipes()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRecipes = _context.Recipes.Where(r => r.UserId == userId).Include(r => r.User).Include(r => r.Ratings).ToList();
            var recipeIds = userRecipes.Select(r => r.Id).ToList();
            var ratings = _context.RecipeRatings.Where(r => recipeIds.Contains(r.RecipeId)).GroupBy(r => r.RecipeId);
            foreach (var recipe in userRecipes)
            {
                recipe.Rating = recipe.Ratings.Any() ? recipe.Ratings.Average(r => r.Rating) : 0;
            }

            double averageRating = 0;
            foreach (var groop in ratings)
            {
                averageRating += groop.Average(r => r.Rating);

            }

            if (ratings.Count() > 0)
            {
                ViewBag.AverageRating = Math.Round(averageRating / ratings.Count(), 1);
            }
            else
            {
                ViewBag.AverageRating = 0;
            }



            return View(userRecipes);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Rate([FromBody] JsonElement body)
        {

            int recipeId = body.GetProperty("recipeId").GetInt32();
            double rating = body.GetProperty("rating").GetDouble();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Validate rating
                if (rating < 1 || rating > 5)
                {
                    Console.WriteLine(rating);
                    Console.WriteLine(recipeId);
                    return Json(new { success = false, message = "Rating must be between 1 and 5" });
                }

                // Check if recipe exists
                var recipe = await _context.Recipes.FindAsync(recipeId);
                if (recipe == null)
                {
                    return Json(new { success = false, message = "Recipe not found" });
                }

                // Check if user already rated this recipe
                var existingRating = await _context.RecipeRatings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.RecipeId == recipeId);

                if (existingRating != null)
                {
                    // Update existing rating
                    existingRating.Rating = rating;
                    _context.RecipeRatings.Update(existingRating);
                }
                else
                {
                    // Create new rating
                    var newRating = new RecipeRating
                    {
                        UserId = userId,
                        RecipeId = recipeId,
                        Rating = rating
                    };
                    _context.RecipeRatings.Add(newRating);
                }

                await _context.SaveChangesAsync();

                // Calculate new average rating
                var ratings = await _context.RecipeRatings
                    .Where(r => r.RecipeId == recipeId)
                    .Select(r => r.Rating)
                    .ToListAsync();

                var averageRating = ratings.Any() ? Math.Round(ratings.Average(), 1) : 0;
                var totalRatings = ratings.Count;

                return Json(new
                {
                    success = true,
                    averageRating = averageRating,
                    totalRatings = totalRatings,
                    message = "Rating submitted successfully"
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                //  _logger?.LogError(ex, "Error rating recipe {RecipeId}", recipeId);

                return Json(new { success = false, message = "An error occurred while submitting your rating" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRating([FromBody] JsonElement body)
        {

            int recipeId = body.GetProperty("recipeId").GetInt32();
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Find the existing rating
                var existingRating = await _context.RecipeRatings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.RecipeId == recipeId);

                if (existingRating == null)
                {
                    return Json(new { success = false, message = "No rating found to remove" });
                }

                _context.RecipeRatings.Remove(existingRating);
                await _context.SaveChangesAsync();

                // Calculate new average rating
                var ratings = await _context.RecipeRatings
                    .Where(r => r.RecipeId == recipeId)
                    .Select(r => r.Rating)
                    .ToListAsync();

                var averageRating = ratings.Any() ? Math.Round(ratings.Average(), 1) : 0;
                var totalRatings = ratings.Count;

                return Json(new
                {
                    success = true,
                    averageRating = averageRating,
                    totalRatings = totalRatings,
                    message = "Rating removed successfully"
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger?.LogError(ex, "Error removing rating for recipe {RecipeId}", recipeId);

                return Json(new { success = false, message = "An error occurred while removing your rating" });
            }
        }

        // Helper method to get rating data for a recipe (add this to your existing methods)
        private async Task<(double averageRating, int totalRatings, double userRating, bool hasUserRated)>
            GetRatingDataAsync(int recipeId, string userId = null)
        {
            var ratings = await _context.RecipeRatings
                .Where(r => r.RecipeId == recipeId)
                .Select(r => new { r.Rating, r.UserId })
                .ToListAsync();

            var averageRating = ratings.Any() ? Math.Round(ratings.Average(r => r.Rating), 1) : 0;
            var totalRatings = ratings.Count;

            double userRating = 0;
            bool hasUserRated = false;

            if (!string.IsNullOrEmpty(userId))
            {
                var userRatingData = ratings.FirstOrDefault(r => r.UserId == userId);
                if (userRatingData != null)
                {
                    userRating = userRatingData.Rating;
                    hasUserRated = true;
                }
            }

            return (averageRating, totalRatings, userRating, hasUserRated);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite([FromBody] JsonElement request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Extract recipeId from the dynamic request
                int recipeId = request.GetProperty("recipeId").GetInt32();

                // Check if recipe exists
                var recipe = await _context.Recipes.FindAsync(recipeId);
                if (recipe == null)
                {
                    return Json(new { success = false, message = "Recipe not found" });
                }

                // Check if already favorited
                var existingFavorite = await _context.FavoriteRecipes
                    .FirstOrDefaultAsync(fr => fr.UserId == userId && fr.RecipeId == recipeId);

                bool isFavorited;

                if (existingFavorite != null)
                {
                    // Remove from favorites
                    _context.FavoriteRecipes.Remove(existingFavorite);
                    isFavorited = false;
                }
                else
                {
                    // Add to favorites
                    var favoriteRecipe = new FavoriteRecipe
                    {
                        UserId = userId,
                        RecipeId = recipeId
                    };
                    _context.FavoriteRecipes.Add(favoriteRecipe);
                    isFavorited = true;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    isFavorited = isFavorited,
                    message = isFavorited ? "Added to favorites" : "Removed from favorites"
                });
            }
            catch (Exception _)
            {
                // Log the error
                // _logger?.LogError(ex, "Error toggling favorite for recipe", ex);

                return Json(new
                {
                    success = false,
                    message = "An error occurred while updating favorites"
                });
            }
        }



        //NEW
        [HttpPost]
        public async Task<IActionResult> AddIngredient([FromBody] JsonElement request)
        {

            String Name = request.GetProperty("Name").GetString();
            float Calories = request.GetProperty("Calories").GetSingle();
            float Protein = request.GetProperty("Protein").GetSingle();
            float Carbs = request.GetProperty("Carbs").GetSingle();
            float Fat = request.GetProperty("Fat").GetSingle();
            // Manually validate the anti-forgery token from headers
            var token = Request.Headers["RequestVerificationToken"].FirstOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Anti-forgery token missing.");
            }
            
            // Validate the model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            // Validate required fields since they might not be caught by model validation
            if (string.IsNullOrWhiteSpace(Name))
            {
                return BadRequest("Ingredient name is required.");
            }
            
            try
            {
                // Check if ingredient already exists
                var existingIngredient = await _context.Ingredients
                    .FirstOrDefaultAsync(i => i.Name.ToLower() == Name.ToLower());
                
                if (existingIngredient != null)
                {
                    return BadRequest("An ingredient with this name already exists.");
                }
                
                // Create new ingredient using your existing model
                var ingredient = new Ingredient
                {
                    Name = Name.Trim(),
                    Calories = Calories,
                    Protein = Protein,
                    Carbs = Carbs,
                    Fat = Fat
                };
                
                _context.Ingredients.Add(ingredient);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    id = ingredient.Id,
                    name = ingredient.Name,
                    calories = ingredient.Calories,
                    protein = ingredient.Protein,
                    carbs = ingredient.Carbs,
                    fat = ingredient.Fat,
                    sucess = true
                });
            }
            catch (Exception ex)
            {
                // Log the exception if you have logging configured
                // _logger?.LogError(ex, "Error adding ingredient: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while adding the ingredient.");
            }
        }


        
            
            
    }

      
} 

