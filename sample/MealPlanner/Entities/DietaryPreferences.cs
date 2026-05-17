using System.ComponentModel.DataAnnotations;

namespace API.Entities;

public class DietaryPreferences
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int DietTypeId { get; set; }
    public DietType DietType { get; set; } = null!;

    public int ServingTypeId { get; set; }
    public ServingType ServingType { get; set; } = null!;

    public ICollection<Allergy> Allergies { get; set; } = [];
}
