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



namespace NutriMatch.Controllers
{


    public class RecipesController : Controller
    {

            float ConvertType(float number, string unit){
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

            return PartialView("_RecipeDetailsPartial", recipe);
        }

        
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

            return PartialView("_RecipeDeletePartial",recipe);
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
