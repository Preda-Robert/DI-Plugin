using System;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class DataContext(DbContextOptions options) : IdentityDbContext<ApplicationUser, ApplicationRole, int, IdentityUserClaim<int>, ApplicationUserRole, IdentityUserLogin<int>, IdentityRoleClaim<int>, IdentityUserToken<int>>(options) //specific order for this one
{
    public DbSet<Cookware> Cookwares { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<IngredientCategory> IngredientCategories { get; set; }
    public DbSet<DietaryPreferences> DietaryPreferences { get; set; }
    public DbSet<MealPlan> MealPlans { get; set; }
    public DbSet<MealPlanRecipe> MealPlanRecipes { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<ShoppingList> ShoppingLists { get; set; }
    public DbSet<ShoppingListItem> ShoppingListItems { get; set; }
    public DbSet<RecipeInstruction> RecipeInstructions { get; set; }
    public DbSet<RecipeCookware> RecipeCookware { get; set; }
    public DbSet<ServingType> ServingTypes { get; set; }
    public DbSet<Allergy> Allergies { get; set; }
    public DbSet<Photo> Photos { get; set; }

    // override conventions for entity framework
    protected override void OnModelCreating(ModelBuilder builder)
    {

         builder.Entity<ApplicationUserRecipe>()
            .HasKey(k => new {k.SourceUserId, k.TargetRecipeId});
        builder.Entity<ApplicationUserRecipe>()
        .HasOne(s => s.SourceUser)
        .WithMany(f => f.FavoriteRecipes)
        .HasForeignKey(s => s.SourceUserId)
        .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Allergy>()
            .HasMany(a => a.DietaryPreferences)
            .WithMany(dp => dp.Allergies)
            .UsingEntity(j => j.ToTable("DietaryPreferencesAllergies"));

        builder.Entity<Cookware>()
            .HasMany(c => c.Recipes)
            .WithOne(rc => rc.Cookware)
            .HasForeignKey(rc => rc.CookwareId)
            .OnDelete(DeleteBehavior.Cascade);



        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>()
            .HasMany(ur => ur.UserRoles)
            .WithOne(u => u.User)
            .HasForeignKey(ur => ur.UserId)
            .IsRequired();

        builder.Entity<ApplicationRole>()
            .HasMany(ur => ur.UserRoles)
            .WithOne(u => u.Role)
            .HasForeignKey(ur => ur.RoleId)
            .IsRequired();


        builder.Entity<ApplicationUser>()
            .HasMany(u => u.ShoppingLists)
            .WithOne(sl => sl.User)
            .HasForeignKey(sl => sl.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ApplicationUser>()
            .HasMany(u => u.MealPlans)
            .WithOne(mp => mp.User)
            .HasForeignKey(mp => mp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ApplicationUser>()
            .HasMany(u => u.Recipes)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Recipe>()
            .HasMany(r => r.Cookware)
            .WithOne(rc => rc.Recipe)
            .HasForeignKey(rc => rc.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Recipe>()
        .HasMany(r => r.Ingredients)
            .WithOne(ri => ri.Recipe)
            .HasForeignKey(ri => ri.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Recipe>()
            .HasMany(r => r.Instructions)
            .WithOne(rc => rc.Recipe)
            .HasForeignKey(rc => rc.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Recipe>()
            .HasOne(r => r.ServingType)
            .WithMany(s => s.Recipes)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ServingType>()
            .HasMany(st => st.Recipes)
            .WithOne(r => r.ServingType)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Ingredient>()
            .HasMany(i => i.RecipeIngredients)
            .WithOne(ri => ri.Ingredient)
            .HasForeignKey(ri => ri.IngredientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Recipe>()
            .HasMany(r => r.Cookware)
            .WithOne(rc => rc.Recipe)
            .HasForeignKey(rc => rc.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Recipe>()
            .HasOne(r => r.User)
            .WithMany(u => u.Recipes)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);


    }
}
