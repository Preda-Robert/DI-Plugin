using System;

namespace API.DTO;

public class ShoppingListDTO
{
        public DateTime CreatedDate { get; set; }
        public ICollection<ShoppingListItemDTO> Items { get; set; } = [];
}
