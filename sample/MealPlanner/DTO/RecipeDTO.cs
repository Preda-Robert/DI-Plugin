using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using API.Entities;

namespace API.DTO;

public class RecipeDTO
{
    public int Id { get; set; }
    [Required]
    public required string Name { get; set; } = string.Empty;
    [Required]
    public required string Description { get; set; } = string.Empty;
    public int CookingTime { get; set; } = 0;
    public string? PhotoUrl { get; set; } = null;
    [Range(0, 5, ErrorMessage = "Rating must be between 0 and 5")]
    public float Rating { get; set; } = 0;
    [JsonPropertyName("Difficulty")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RecipeDifficulty Difficulty { get; set; } = RecipeDifficulty.Easy;
    public ServingTypeDTO ServingType { get; set; } = null!;
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    public ICollection<AllergyDTO> Allergies { get; set; } = [];
    public ICollection<CookwareDTO> Cookware { get; set; } = [];
    public ICollection<RecipeInstructionDTO> Instructions { get; set; } = [];
    public ICollection<RecipeIngredientDTO> Ingredients { get; set; } = [];
}
