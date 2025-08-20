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
        public float Calories { get; set; }
        public float Protein { get; set; }
        public float Carbs { get; set; }
        public float Fat { get; set; }

        public String? Status { get; set; }

    }
}