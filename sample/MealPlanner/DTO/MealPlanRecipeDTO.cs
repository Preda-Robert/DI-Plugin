using System;

namespace API.DTO;

public class MealPlanRecipeDTO
{
    public int Id { get; set; }
    public ServingTypeDTO ServingType { get; set; } = null!;
    public RecipeDTO Recipe { get; set; } = null!;
}