using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NutriMatch.Models
{
    public class RecipeIngredient
    {
        [Key]
        public int Id { get; set; }

        public int RecipeId { get; set; }
        public int IngredientId { get; set; }

        public virtual Ingredient Ingredient {get;set;}

        public String Unit { get; set; }

        public float Quantity { get; set; }

        [NotMapped]
        public NutritionInfo NutritionInfo { get; set; } 
    }
}