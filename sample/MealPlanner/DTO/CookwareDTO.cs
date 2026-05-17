using System;

namespace API.DTO;

public class CookwareDTO
{
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public string? PhotoUrl { get; set; }
}
