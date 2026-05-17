using System;

namespace API.Helpers;

public class IngredientParams : PaginationParams
{
    public string? SearchTerm { get; set; }
    public List<int> AllergyIds { get; set; } = new List<int>();
}
