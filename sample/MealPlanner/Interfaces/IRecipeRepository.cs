using System;
using API.Entities;
using API.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace API.Interfaces;

public interface IRecipeRepository : IBaseRepository<Recipe>
{
    Task<Recipe?> GetRecipeByIdAsync(int id);
    Task<ICollection<Recipe>?> GetRecipesAsync(RecipeParams recipeParams);
    Task<ICollection<Recipe>?> GetRecipesByUserIdAsync(string userId);
    Task<Recipe?> GetRecipeByNameAsync(string name);
    IQueryable<Recipe> GetRecipes(RecipeParams recipeParams);
}
