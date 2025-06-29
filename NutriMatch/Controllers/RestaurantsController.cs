using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NutriMatch.Data;

namespace NutriMatch.Controllers
{

    public class RestaurantsController : Controller
    {
        private readonly AppDbContext _context;


        public RestaurantsController(AppDbContext context)
        {

            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var restaurants = await _context.Restaurants.ToListAsync();
            return View(restaurants);
        }
        

        public async Task<IActionResult> GetRestaurantMeals(int? id,int? minCalories, int? maxCalories, int? minProtein, int? maxProtein, int? minCarbs, int? maxCarbs, int? minFat, int? maxFat)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants.Include(r => r.RestaurantMeals)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (restaurant == null)
            {
                return NotFound();
            }



            var filteredMeals = restaurant.RestaurantMeals
        .Where(r =>
            (minCalories == null || r.Calories >= minCalories) &&
            (maxCalories == null || r.Calories <= maxCalories) &&
            (minProtein == null || r.Protein >= minProtein) &&
            
            (maxProtein == null || r.Protein <= maxProtein) &&
            (minFat == null || r.Fat >= minFat) &&
            (maxFat == null || r.Fat <= maxFat) &&
            (minCarbs == null || r.Carbs >= minCarbs) &&
            (maxCarbs == null || r.Carbs <= maxCarbs)
        )
        .ToList();

            Console.WriteLine($"Total meals for restaurant {id}: {filteredMeals.Count}");


            return PartialView("_RestaurantMealsPartial", filteredMeals);
        }

        
        
    }
}