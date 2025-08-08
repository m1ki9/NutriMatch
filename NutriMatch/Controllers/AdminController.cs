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
            .Where(r => r.IsApproved == false)
            .Include(r => r.User)
            .ToListAsync();
        return View(pendingRecipes);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRecipe([FromBody] int recipeId)
    {
        var recipe = await _context.Recipes.FindAsync(recipeId);
        if (recipe == null)
            return NotFound("Recipe not found.");
        recipe.IsApproved = true;
        await _context.SaveChangesAsync();
        return Ok("Recipe approved successfully.");
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineRecipe([FromBody] int recipeId)
    {
        var recipe = await _context.Recipes.FindAsync(recipeId);
        if (recipe == null)
            return NotFound("Recipe not found.");
        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync();
        return Ok("Recipe declined successfully.");
    }
    [HttpPost]
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
            recipe.IsApproved = true;
            Console.WriteLine(recipe.Title + " approved");
        }
        await _context.SaveChangesAsync();
        return Json(new { message = "Selected recipes approved.", success = true }); ;
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDeclineRecipes([FromBody] List<int> recipeIds)
    {
        var recipes = await _context.Recipes
            .Where(r => recipeIds.Contains(r.Id))
            .ToListAsync();
        foreach (var recipe in recipes)
        {
            _context.Recipes.Remove(recipe);
        }
        await _context.SaveChangesAsync();
        return Ok("Selected recipes declined.");
    }
}