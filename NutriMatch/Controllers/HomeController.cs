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
            var model = new HomeViewModel
            {
                Recipes = await _context.Recipes.Take(6).ToListAsync(),
                Restaurants = await _context.Restaurants.ToListAsync()                
            };
            return View(model);
        }


    }
}
