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
namespace NutriMatch.Controllers
{
    public class RecipesController : Controller
    {
        private readonly AppDbContext _context;
        public RecipesController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _context.Recipes.ToListAsync());
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var recipe = await _context.Recipes.Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }
            return View(recipe);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Instructions")] Recipe recipe)
        {
            if (ModelState.IsValid)
            {
                _context.Add(recipe);
                await _context.SaveChangesAsync();
                string selectedIngredients = Request.Form["Ingredients"];
                List<SelectedIngredient> ingredients = JsonSerializer.Deserialize<List<SelectedIngredient>>(selectedIngredients);
                foreach(var i in ingredients)
                { 
                    _context.RecipeIngredients.Add(new RecipeIngredient {
                        RecipeId = recipe.Id,
                        IngredientId = i.Id,
                        Unit = i.Unit,
                        Quantity = i.Quantity 
                    });
                }
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
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return NotFound();
            }
            return View(recipe);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Instructions")] Recipe recipe)
        {
            if (id != recipe.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(recipe);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecipeExists(recipe.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(recipe);
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var recipe = await _context.Recipes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }
            return View(recipe);
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
    }
}
