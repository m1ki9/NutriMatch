using System.ComponentModel.DataAnnotations;

namespace NutriMatch.Models
{
    public class Restaurant
    {
        [Key]
        public int Id { get; set; }

        public String Name { get; set; }

        public String ImageUrl { get; set; }

        virtual public List<RestaurantMeal> RestaurantMeals { get; set; }
    }
}