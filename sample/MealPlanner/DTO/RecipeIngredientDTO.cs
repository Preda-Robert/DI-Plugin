using System;
using API.Entities;

namespace API.DTO;

public class RecipeIngredientDTO
{
        public float Quantity { get; set; }
        public IngredientDTO Ingredient { get; set; } = null!;
}
