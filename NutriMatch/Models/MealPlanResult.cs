
using NutriMatch.Models;

namespace NutriMatch.Models
{
    public class MealPlanResult
    {
        public WeeklyMealPlan WeeklyMealPlan { get; set; }
        public Dictionary<string, DailyMacros> DailyMacroTotals { get; set; } = new Dictionary<string, DailyMacros>();
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}