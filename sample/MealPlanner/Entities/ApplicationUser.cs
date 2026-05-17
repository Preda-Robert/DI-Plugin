using Microsoft.AspNetCore.Identity;

namespace API.Entities;

public class ApplicationUser : IdentityUser<int>
{
        public string DisplayName { get; set; } = string.Empty;
        public Photo? Photo { get; set; } 
        public ICollection<ApplicationUserRole> UserRoles { get; set; } = [];
        public ICollection<MealPlan> MealPlans { get; set; } = [];
        public ICollection<ShoppingList> ShoppingLists { get; set; } = [];
        public ICollection<Recipe> Recipes { get; set; } = [];
        public ICollection<ApplicationUserRecipe> FavoriteRecipes { get; set; } = [];
        public DietaryPreferences DietaryPreferences { get; set; } = null!;
        public bool HasDoneSetup { get; set; } = false;
        public string? EmailVerificationCode { get; set; }
        public DateTime? EmailVerificationCodeExpiry { get; set; }
        public string? PasswordResetCode { get; set; }
        public DateTime? PasswordResetCodeExpiry { get; set; }


}
