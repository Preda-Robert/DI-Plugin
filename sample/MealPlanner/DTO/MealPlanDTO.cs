using System;
using System.ComponentModel.DataAnnotations;
using API.Entities;

namespace API.DTO;

public class MealPlanDTO
{
    public int Id { get; set; }
    [Required]
    public required string Name { get; set; }
    [Required]
    public required string Description { get; set; }
    public string? PhotoUrl { get; set; }
    public ICollection<MealPlanRecipeDTO> MealPlanRecipes { get; set; } = [];
}