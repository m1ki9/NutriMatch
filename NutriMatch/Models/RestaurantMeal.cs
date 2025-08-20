using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NutriMatch.Models
{
    public class RestaurantMeal
    {
        [Key]
        public int Id { get; set; }

        virtual public int? RestaurantId { get; set; }
        virtual public Restaurant Restaurant { get; set; } = null!;
        public String RestaurantName { get; set; }

        public String ItemName { get; set; }

        public String ItemDescription { get; set; }

        public List<String> Type { get; set; } = new List<String>(){""};
        
        public float Calories { get; set; }
        public float Protein { get; set; }
        public float Carbs { get; set; }
        public float Fat { get; set; }


    }
}