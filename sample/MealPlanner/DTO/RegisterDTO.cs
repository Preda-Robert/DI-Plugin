using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class RegisterDTO
{
    [StringLength(32, MinimumLength = 2)]
    public required string Username { get; set; } = string.Empty;
    [StringLength(32, MinimumLength = 2)]
    public required string DisplayName { get; set; } = string.Empty;
    [EmailAddress]
    public required string Email { get; set; } = string.Empty;
    [StringLength(32, MinimumLength = 8)]
    public required string Password { get; set; } = string.Empty; 
}