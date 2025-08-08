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
    public class RecipesController : Controller
    {
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
            var recipes = await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.Ratings)
                .ToListAsync();
            foreach (var recipe in recipes)
            {
                recipe.Rating = recipe.Ratings.Any() ? recipe.Ratings.Average(r => r.Rating) : 0;
            }
            List<int> favoriteRecipeIds = new List<int>();
            if (!string.IsNullOrEmpty(userId))
            {
                favoriteRecipeIds = await _context.FavoriteRecipes
                    .Where(fr => fr.UserId == userId)
                    .Select(fr => fr.RecipeId)
                    .ToListAsync();
            }
            ViewBag.FavoriteRecipeIds = favoriteRecipeIds;
            ViewBag.userId = userId;
            return View(recipes);
        }
        [Route("Recipes/Details/{id}/{isOwner?}")]
        public async Task<IActionResult> Details(int? id, bool isOwner = false)
        {
            if (id == null)
            {
                return NotFound();
            }
            var recipe = await _context.Recipes.Include(r => r.User)
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(m => m.Id == id);      
            if (recipe == null)
            {
                return NotFound();
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool actualIsOwner = !string.IsNullOrEmpty(userId) && recipe.UserId == userId;
            var (averageRating, totalRatings, userRating, hasUserRated) = 
                await GetRatingDataAsync(id.Value, userId);
            bool isFavorited = false;
            if (!string.IsNullOrEmpty(userId))
            {
                isFavorited = await _context.FavoriteRecipes
                    .AnyAsync(fr => fr.UserId == userId && fr.RecipeId == id.Value);
            }
            ViewBag.IsOwner = actualIsOwner;
            ViewBag.AverageRating = averageRating;
            ViewBag.TotalRatings = totalRatings;
            ViewBag.UserRating = userRating;
            ViewBag.HasUserRated = hasUserRated;
            ViewBag.IsFavorited = isFavorited;
            return PartialView("_RecipeDetailsPartial", recipe);
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
        public async Task<IActionResult> Edit(int? id)
        {
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
            ViewBag.AverageRating = Math.Round(averageRating / ratings.Count(), 1);
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
                if (rating < 1 || rating > 5)
                {
                    Console.WriteLine(rating);
                    Console.WriteLine(recipeId);
                    return Json(new { success = false, message = "Rating must be between 1 and 5" });
                }
                var recipe = await _context.Recipes.FindAsync(recipeId);
                if (recipe == null)
                {
                    return Json(new { success = false, message = "Recipe not found" });
                }
                var existingRating = await _context.RecipeRatings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.RecipeId == recipeId);
                if (existingRating != null)
                {
                    existingRating.Rating = rating;
                    _context.RecipeRatings.Update(existingRating);
                }
                else
                {
                    var newRating = new RecipeRating
                    {
                        UserId = userId,
                        RecipeId = recipeId,
                        Rating = rating
                    };
                    _context.RecipeRatings.Add(newRating);
                }
                await _context.SaveChangesAsync();
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
                var existingRating = await _context.RecipeRatings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.RecipeId == recipeId);
                if (existingRating == null)
                {
                    return Json(new { success = false, message = "No rating found to remove" });
                }
                _context.RecipeRatings.Remove(existingRating);
                await _context.SaveChangesAsync();
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
                return Json(new { success = false, message = "An error occurred while removing your rating" });
            }
        }
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
                int recipeId = request.GetProperty("recipeId").GetInt32();
                var recipe = await _context.Recipes.FindAsync(recipeId);
                if (recipe == null)
                {
                    return Json(new { success = false, message = "Recipe not found" });
                }
                var existingFavorite = await _context.FavoriteRecipes
                    .FirstOrDefaultAsync(fr => fr.UserId == userId && fr.RecipeId == recipeId);
                bool isFavorited;
                if (existingFavorite != null)
                {
                    _context.FavoriteRecipes.Remove(existingFavorite);
                    isFavorited = false;
                }
                else
                {
                    var favoriteRecipe = new FavoriteRecipe
                    {
                        UserId = userId,
                        RecipeId = recipeId
                    };
                    _context.FavoriteRecipes.Add(favoriteRecipe);
                    isFavorited = true;
                }
                await _context.SaveChangesAsync();
                return Json(new { 
                    success = true, 
                    isFavorited = isFavorited,
                    message = isFavorited ? "Added to favorites" : "Removed from favorites"
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error toggling favorite for recipe", ex);
                return Json(new { 
                    success = false, 
                    message = "An error occurred while updating favorites" 
                });
            }
        }
    }
} 
