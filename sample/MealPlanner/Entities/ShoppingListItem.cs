using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities;

  public class ShoppingListItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public IngredientMeasurementType MeasurementType { get; set; } = IngredientMeasurementType.None;
        public bool IsPurchased { get; set; }

        public int ShoppingListId { get; set; }
        public ShoppingList ShoppingList { get; set; } = null!;
        public int IngredientId { get; set; }  
        public Ingredient Ingredient { get; set; } = null!;
    }
