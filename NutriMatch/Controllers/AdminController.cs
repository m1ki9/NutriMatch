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

    public AdminController(AppDbContext context)
    {
        _context = context;
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
        int recipeId = request.GetProperty("recipeId").GetInt32();
        var recipe = await _context.Recipes.FindAsync(recipeId);
        if (recipe == null)
            return NotFound("Recipe not found.");

        recipe.RecipeStatus = "Accepted";
        await _context.SaveChangesAsync();

        return Json(new { message = "Selected recipe approved.", success = true }); ;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineRecipe([FromBody] JsonElement request)
    {
        int recipeId = request.GetProperty("recipeId").GetInt32();
        string reason = request.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : "No reason provided.";
        string notes = request.TryGetProperty("notes", out var notesProp) ? notesProp.GetString() : "No notes provided.";

        var recipe = await _context.Recipes.FindAsync(recipeId);
        if (recipe == null)
            return NotFound("Recipe not found.");

        recipe.RecipeStatus = "Declined";
        recipe.DeclineReason = reason ?? string.Empty;
        recipe.AdminComment = notes ?? string.Empty;
        // _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync();

        return Json(new { message = "Selected recipe declined.", success = true }); ;
    }

    [HttpPost]
    // [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkApproveRecipes([FromBody] JsonElement request)
    {
        List<int> recipeIds = request.GetProperty("recipeIds").EnumerateArray()
            .Select(x => x.GetInt32())
            .ToList();

        var recipes = await _context.Recipes
            .Where(r => recipeIds.Contains(r.Id))
            .ToListAsync();

        foreach (var recipe in recipes)
        {
            recipe.RecipeStatus = "Accepted";
            Console.WriteLine(recipe.Title + " approved");
        }

        await _context.SaveChangesAsync();

        return Json(new { message = "Selected recipes approved.", success = true }); ;

    }

    public async Task<IActionResult> DeclineReasonModel(int? id)
    {
        var recipe = await _context.Recipes.Include(r => r.User)
           .Include(r => r.RecipeIngredients)
           .ThenInclude(ri => ri.Ingredient)
           .FirstOrDefaultAsync(m => m.Id == id);


        return PartialView("_RecipeDeclineAdminPartial", recipe);
    }
    






}