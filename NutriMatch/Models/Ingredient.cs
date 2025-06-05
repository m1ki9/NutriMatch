using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace NutriMatch.Models
{
    public class Ingredient
    {
        [Key]
        public int Id { get; set; }

        public String Name { get; set; }

        public String Unit { get; set; }

        public float Quantity { get; set; }

        [NotMapped]
        public NutritionInfo NutritionInfo { get; set; } 
    }
}