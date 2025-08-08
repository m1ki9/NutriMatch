using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutriMatch.Data;
using NutriMatch.Models;

namespace MyApp.Namespace
{
    public class Home : Controller
    {

        private readonly AppDbContext _context;
        public Home(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {   
            var recipes = await _context.Recipes.Include(r => r.User).Include(r => r.Ratings).Take(6).ToListAsync();
            foreach (var recipe in recipes)
            {
                recipe.Rating = recipe.Ratings.Any() ? recipe.Ratings.Average(r => r.Rating) : 0;
            }
            var model = new HomeViewModel
            {

               

                Recipes = recipes,
                Restaurants = await _context.Restaurants.ToListAsync()                
            };
            return View(model);
        }


    }
}
