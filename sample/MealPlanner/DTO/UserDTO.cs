using System;

namespace API.DTO;

public class UserDTO
{
        public int Id { get; set; }
        public required string UserName { get; set; }
        public required string DisplayName { get; set; }
        public bool HasDoneSetup { get; set; } = false;
        public required string Token { get; set; }
        public string? PhotoUrl { get; set; }
        public bool EmailConfirmed { get; set; } 
}
