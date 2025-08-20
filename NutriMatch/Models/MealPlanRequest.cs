using System.ComponentModel.DataAnnotations;

namespace NutriMatch.Models
{
public class MealPlanRequest
    {
        [Required]
        [Range(1200, 5000, ErrorMessage = "Daily calories must be between 1200 and 5000")]
        public float DailyCalories { get; set; }

        [Required]
        [Range(50, 300, ErrorMessage = "Daily protein must be between 50g and 300g")]
        public float DailyProtein { get; set; }

        [Required]
        [Range(100, 600, ErrorMessage = "Daily carbs must be between 100g and 600g")]
        public float DailyCarbs { get; set; }

        [Required]
        [Range(30, 200, ErrorMessage = "Daily fat must be between 30g and 200g")]
        public float DailyFat { get; set; }

        [Required]
        [Range(0, 21, ErrorMessage = "Restaurant meals per week must be between 0 and 21")]
        public int RestaurantMealsPerWeek { get; set; }

        public List<string> PreferredMealTypes { get; set; } = new List<string>();
        public List<string> DietaryRestrictions { get; set; } = new List<string>();
    }
}