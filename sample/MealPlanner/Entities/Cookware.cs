using System.ComponentModel.DataAnnotations;

namespace API.Entities;

 public class Cookware
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public Photo? Photo { get; set; } = null!;
        public ICollection<RecipeCookware> Recipes { get; set; } = [];

    }