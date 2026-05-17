using System;

namespace API.Entities;

public class RecipeInstruction
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int InstructionIndex { get; set; }
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
}
