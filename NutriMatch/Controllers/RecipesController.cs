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
            float ConvertType(float number, string unit)
            {
                switch (unit.ToLower())
                {
                    case "g":
                        return number / 100; 
                    case "ml":
                        return number / 100; 
                    case "oz":
                        return (float)(number * 28.3495 / 100); 
                    default:
                        return 0;
                }
            }
            if (ModelState.IsValid)
            {
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
                    totalCalories += ConvertType(tempIngredient.Calories,i.Unit) * i.Quantity;
                    totalProtein += ConvertType(tempIngredient.Protein,i.Unit) * i.Quantity;
                    totalCarbs += ConvertType(tempIngredient.Carbs,i.Unit) * i.Quantity;
                    totalFat += ConvertType(tempIngredient.Fat,i.Unit) * i.Quantity;
                }
                recipe.Calories = totalCalories;
                recipe.Protein = totalProtein;
                recipe.Carbs = totalCarbs;
                recipe.Fat = totalFat;
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
        public async Task<ActionResult<List<Recipe>>> Filter()
        {
            string minCalories = Request.Form["MinCalories"];
            var maxCalories = Request.Form["MaxCalories"];
            var minProtein = Request.Form["MinProtein"];
            var maxProtein = Request.Form["MaxProtein"];
            var minFats = Request.Form["MinFats"];
            var maxFats = Request.Form["MaxFats"];
            var minCarbs = Request.Form["MinCarbs"];
            var maxCarbs = Request.Form["MaxCarbs"];
            var filteredRecipes = _context.Recipes
            .Where(r =>
            (r.Calories >= int.Parse(minCalories)) &&
            (r.Calories <= int.Parse(maxCalories)) &&
            (r.Protein >= int.Parse(minProtein)) &&
            (r.Protein <= int.Parse(maxProtein)) &&
            (r.Fat >= int.Parse(minFats)) &&
            (r.Fat <= int.Parse(maxFats)) &&
            (r.Carbs >= int.Parse(minCarbs)) &&
            (r.Carbs <= int.Parse(maxCarbs))
            )
            .ToList();
            return filteredRecipes;
        }
    } 
}
