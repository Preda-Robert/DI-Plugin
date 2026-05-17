using System;

namespace API.Entities;

public class ApplicationUserRecipe
{
    public ApplicationUser SourceUser { get; set; } = null!;
    public int SourceUserId { get; set; }
    public Recipe TargetRecipe { get; set; } = null!;
    public int TargetRecipeId { get; set; }
}
