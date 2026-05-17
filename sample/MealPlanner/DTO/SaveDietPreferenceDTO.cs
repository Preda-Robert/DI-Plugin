using System;

namespace API.DTO;

public class SaveDietPreferenceDTO
{
    public int UserId { get; set; }
    public int DietTypeId { get; set; }
    public int ServingTypeId { get; set; }
    public ICollection<int> AllergyIds { get; set; } = [];
}
