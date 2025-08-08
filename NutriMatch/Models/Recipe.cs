using System;   
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace NutriMatch.Models
{
    public class Recipe
    {
        [Key]
        public int Id { get; set; }
        public String Title { get; set; }
        public String[]? Instructions { get; set; }
        [ValidateNever]
        [NotMapped]
        public double Rating { get; set; } 
        [ValidateNever]
        public virtual List<RecipeIngredient> RecipeIngredients { get; set; }
        [ValidateNever]
        public float Calories { get; set; }
        [ValidateNever]
        public float Protein { get; set; }
        [ValidateNever]
        public float Carbs { get; set; }
        [ValidateNever]
        public float Fat { get; set; }
        [ValidateNever]
        public String ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now.ToUniversalTime();
        public bool IsApproved { get; set; } = false;

        [ValidateNever]
        public String UserId { get; set; }
        [ValidateNever]
        public virtual User User { get; set; }
        [ValidateNever]
        public ICollection<FavoriteRecipe> FavoritedBy { get; set; }
        [ValidateNever]
        public ICollection<RecipeRating> Ratings { get; set; }
    }
}