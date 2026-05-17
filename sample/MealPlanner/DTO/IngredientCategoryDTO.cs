using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO;

public class IngredientCategoryDTO
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [JsonIgnore]
    public ICollection<IngredientDTO> Ingredients { get; set; } = [];
}