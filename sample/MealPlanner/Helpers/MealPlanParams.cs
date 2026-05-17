using System;

namespace API.Helpers;

public class MealPlanParams : PaginationParams
{
    public string? SearchTerm { get; set; }
}
