using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace NutriMatch.Models
{
    public class User : IdentityUser
    {
        public virtual ICollection<Recipe> Recipes { get; set; }

        public String ProfilePictureUrl { get; set; }
        public ICollection<FavoriteRecipe> FavoriteRecipes { get; set; }
        public ICollection<RecipeRating> Ratings { get; set; }
        
    }
}