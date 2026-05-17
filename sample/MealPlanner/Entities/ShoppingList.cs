using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities;


    public class ShoppingList
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public ICollection<ShoppingListItem> Items { get; set; } = [];
    }
