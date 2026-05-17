using System;

namespace API.DTO;

public class EmailConfirmationDTO
{
    public int Id { get; set; }
    public required string Token { get; set; }
}
