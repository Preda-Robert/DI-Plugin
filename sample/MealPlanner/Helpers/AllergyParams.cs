using System;

namespace API.Helpers;

public class AllergyParams : PaginationParams
{
    public string? SearchTerm { get; set; }
}
