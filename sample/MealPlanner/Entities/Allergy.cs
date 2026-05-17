using System;

namespace API.Entities;

public class Allergy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<DietaryPreferences> DietaryPreferences { get; set; } = [];

}
