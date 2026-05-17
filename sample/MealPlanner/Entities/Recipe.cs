using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API.DTO;

namespace API.Entities;

 public class Recipe
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int CookingTime { get; set; } = 0;
        public ICollection<Photo> Photos { get; set; } = [];
        public float Rating { get; set; } = 0;
        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public RecipeDifficulty Difficulty { get; set; } = RecipeDifficulty.Easy;
        public ServingType ServingType { get; set; } = null!;
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        public ICollection<Allergy> Allergies { get; set; } = [];
        public ICollection<RecipeCookware> Cookware { get; set; } = [];
        public ICollection<RecipeInstruction> Instructions { get; set; } = [];
        public ICollection<RecipeIngredient> Ingredients { get; set; } = [];
    }