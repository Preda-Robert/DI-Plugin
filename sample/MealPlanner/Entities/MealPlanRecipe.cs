using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities;

public class MealPlanRecipe
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; } = null!;
        public int MealPlanId { get; set; }
        public MealPlan MealPlan { get; set; } = null!;
        public ServingType ServingType { get; set; } = null!;
    }