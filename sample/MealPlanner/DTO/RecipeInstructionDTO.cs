using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.DTO;

public class RecipeInstructionDTO
{
    [Required(ErrorMessage = "Description cannot be blank!")]
    public string Description { get; set; } = string.Empty;
    public int InstructionIndex { get; set; } = 0;
}
