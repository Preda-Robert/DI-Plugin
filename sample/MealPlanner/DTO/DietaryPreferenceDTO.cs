using System;

namespace API.DTO;

public class DietaryPreferenceDTO
{
    public int Id { get; set; }
    public DietTypeDTO DietType { get; set; } = null!;
    public ServingTypeDTO ServingType { get; set; } = null!;
    public ICollection<AllergyDTO> Allergies { get; set; } = new List<AllergyDTO>();
}
