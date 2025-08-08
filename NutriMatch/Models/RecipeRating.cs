using System.ComponentModel.DataAnnotations;

namespace NutriMatch.Models
{
    public class RecipeRating
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }

        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; }

        [Range(1.0, 5.0)]
        public double Rating { get; set; }
    }

}