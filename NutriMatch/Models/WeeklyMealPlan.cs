using System.ComponentModel.DataAnnotations;

namespace NutriMatch.Models
{
    public class WeeklyMealPlan
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public List<MealSlot> MealSlots { get; set; } = new List<MealSlot>();
        public DateTime GeneratedAt { get; set; } = DateTime.Now.ToUniversalTime();
    }
}