using System;   
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public String Instructions { get; set; }
        public float Rating { get; set; }
        [ValidateNever]
        public virtual List<Ingredient> Ingredients { get; set; }   
        
    }
}