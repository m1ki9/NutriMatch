using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriMatch.Models;
using NutriMatch.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AppDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var pendingRecipes = await _context.Recipes
            .Where(r => r.RecipeStatus == "Pending")
            .Include(r => r.User)
            .ToListAsync();

        return View(pendingRecipes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRecipe([FromBody] JsonElement request)
    {
        try
        {
            if (!request.TryGetProperty("recipeId", out var recipeIdProp))
            {
                return Json(new { success = false, message = "Recipe ID is required." });
            }

            int recipeId = recipeIdProp.GetInt32();


            var recipe = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.Id == recipeId);

            if (recipe == null)
            {
                return Json(new { success = false, message = "Recipe not found." });
            }

            recipe.RecipeStatus = "Accepted";

            if (recipe.HasPendingIngredients == true)
            {
                var pendingIngredients = recipe.RecipeIngredients
                    .Where(ri => ri.Ingredient.Status == "Pending")
                    .Select(ri => ri.Ingredient);

                foreach (var ingredient in pendingIngredients)
                {
                    ingredient.Status = null;
                }

                recipe.HasPendingIngredients = false;
            }

            await _context.SaveChangesAsync();

            return Json(new { message = "Recipe approved successfully.", success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving recipe");
            return Json(new { success = false, message = "An error occurred while approving the recipe." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineRecipe([FromBody] JsonElement request)
    {
        try
        {
            if (!request.TryGetProperty("recipeId", out var recipeIdProp))
            {
                return Json(new { success = false, message = "Recipe ID is required." });
            }

            int recipeId = recipeIdProp.GetInt32();
            string reason = request.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : "No reason provided.";
            string notes = request.TryGetProperty("notes", out var notesProp) ? notesProp.GetString() : "No notes provided.";

            var recipe = await _context.Recipes.FindAsync(recipeId);
            if (recipe == null)
            {
                return Json(new { success = false, message = "Recipe not found." });
            }

            recipe.RecipeStatus = "Declined";
            recipe.DeclineReason = reason ?? string.Empty;
            recipe.AdminComment = notes ?? string.Empty;

            await _context.SaveChangesAsync();

            return Json(new { message = "Recipe declined successfully.", success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining recipe");
            return Json(new { success = false, message = "An error occurred while declining the recipe." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkApproveRecipes([FromBody] JsonElement request)
    {
        try
        {
            if (!request.TryGetProperty("recipeIds", out var recipeIdsProp))
            {
                return Json(new { success = false, message = "Recipe IDs are required." });
            }

            List<int> recipeIds = recipeIdsProp.EnumerateArray()
                .Select(x => x.GetInt32())
                .ToList();

            if (!recipeIds.Any())
            {
                return Json(new { success = false, message = "No recipe IDs provided." });
            }

            var recipes = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .Where(r => recipeIds.Contains(r.Id))
                .ToListAsync();

            if (!recipes.Any())
            {
                return Json(new { success = false, message = "No recipes found." });
            }

            int approvedCount = 0;
            foreach (var recipe in recipes)
            {
                recipe.RecipeStatus = "Accepted";

                if (recipe.HasPendingIngredients == true)
                {
                    var pendingIngredients = recipe.RecipeIngredients
                        .Where(ri => ri.Ingredient.Status == "Pending")
                        .Select(ri => ri.Ingredient);

                    foreach (var ingredient in pendingIngredients)
                    {
                        ingredient.Status = null;
                    }

                    recipe.HasPendingIngredients = false;
                }

                approvedCount++;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                message = $"{approvedCount} recipe(s) approved successfully.",
                success = true,
                approvedCount = approvedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk approving recipes");
            return Json(new { success = false, message = "An error occurred while approving recipes." });
        }
    }

    public async Task<IActionResult> DeclineReasonModel(int? id)
    {
        try
        {
            if (id == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (recipe == null)
            {
                return NotFound();
            }

            return PartialView("_RecipeDeclineAdminPartial", recipe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading decline reason model");
            return StatusCode(500, "An error occurred while loading the decline form.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetIngredientReview(int id)
    {
        try
        {
            var ingredient = await _context.Ingredients
                .Where(i => i.Id == id && i.Status == "Pending")
                .FirstOrDefaultAsync();

            if (ingredient == null)
            {
                return NotFound("Ingredient not found or not pending review.");
            }

            return PartialView("_IngredientReviewPartial", ingredient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading ingredient review for ID: {IngredientId}", id);
            return StatusCode(500, "An error occurred while loading ingredient details.");
        }
    }
}