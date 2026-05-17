using System;
using Google.Apis.Http;

namespace API.Helpers;

public class RecipeParams : PaginationParams
{
    public string? SearchTerm { get; set; }
    public int UserId { get; set; }
}
