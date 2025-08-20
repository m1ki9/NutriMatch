using System;   
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
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
        public String RecipeStatus { get; set; } = "Pending";
        public String AdminComment { get; set; } = String.Empty;
        public String DeclineReason { get; set; } = String.Empty;
        public bool? HasPendingIngredients { get; set; }
        [ValidateNever]
        public String UserId { get; set; }
        [ValidateNever]
        [JsonIgnore]
        public virtual User User { get; set; }
        [ValidateNever]
        public ICollection<FavoriteRecipe> FavoritedBy { get; set; }
        [ValidateNever]
        public ICollection<RecipeRating> Ratings { get; set; }
        [ValidateNever]
        public List<String> Type { get; set; }
    }
}