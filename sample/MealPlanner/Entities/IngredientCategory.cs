using System;
using System.Text.Json.Serialization;

namespace API.Entities;

public class IngredientCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [JsonIgnore]
    public ICollection<Ingredient> Ingredients { get; set; } = null!;
}
