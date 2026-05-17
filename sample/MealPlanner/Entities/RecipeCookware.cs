using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities;

public class RecipeCookware
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; } = null!;   
        public int CookwareId { get; set; }
        public Cookware Cookware { get; set; } = null!;
    }
