using System;
using System.Text.Json.Serialization;
using API.Entities;

namespace API.DTO;

public class ShoppingListItemDTO
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public IngredientDTO Ingredient { get; set; } = null!;
    public bool IsPurchased { get; set; }
}