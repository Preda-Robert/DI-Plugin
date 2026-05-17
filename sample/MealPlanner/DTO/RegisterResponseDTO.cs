using System;

namespace API.DTO;

public class RegisterResponseDTO
{
    public bool IsRegistered { get; set; } = false;
    public bool RequiresEmailConfirmation { get; set; } = false;
    public int? UserId { get; set; } = null;
}
