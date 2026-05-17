using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTO;

public class DietTypeDTO
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Name cannot be empty")]
    public string Name { get; set; } = string.Empty;
}
