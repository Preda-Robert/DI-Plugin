using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities;
[Table("Ingredients")]
public class Ingredient
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int CategoryId { get; set; }
    public IngredientCategory Category { get; set; } = null!;
    public bool IsAllergen { get; set; } = false;
    public int? AllergyId { get; set; }
    public Allergy? Allergy { get; set; }
    public IngredientMeasurementType MeasurementType { get; set; } = IngredientMeasurementType.None;
    public float Calories { get; set; }
    public Photo? Photo { get; set; }

    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = [];
}
