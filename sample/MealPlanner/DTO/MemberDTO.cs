using System;
using API.Entities;

namespace API.DTO;

public class MemberDTO
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public DietaryPreferenceDTO DietaryPreference { get; set; } = null!;
    public ICollection<RecipeDTO> CreatedRecipes { get; set; } = [];
    public ICollection<RecipeDTO> FavoriteRecipes { get; set; } = [];
    public ICollection<MealPlanDTO> MealPlans { get; set; } = [];

}
