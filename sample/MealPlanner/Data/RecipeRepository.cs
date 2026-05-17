using System;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class RecipeRepository : BaseRepository<Recipe>, IRecipeRepository
{
    public RecipeRepository(DataContext context) : base(context)
    {
    }

    public async Task<Recipe?> GetRecipeByIdAsync(int id)
    {
        return await _context.Recipes.FindAsync(id);
    }

    public Task<ICollection<Recipe>?> GetRecipesAsync(RecipeParams recipeParams)
    {
        return null!;
    }

    public Task<ICollection<Recipe>?> GetRecipesByUserIdAsync(string userId)
    {
        return null!;
    }

    public async Task<Recipe?> GetRecipeByNameAsync(string name)
    {
        return await _context.Recipes.FirstOrDefaultAsync(r => r.Name == name);
    }

    public IQueryable<Recipe> GetRecipes(RecipeParams recipeParams)
    {
        var query = _context.Recipes.AsQueryable();
        var userId = recipeParams.UserId;
        
        if (userId > 0)
        {
            query = query.Where(r => r.User.Id == userId);
        }

        if (!string.IsNullOrEmpty(recipeParams.SearchTerm))
        {
            query = query.Where(a => a.Name.ToLower().Contains(recipeParams.SearchTerm.ToLower()));
        }

        query = query.OrderBy(a => a.Name);

        return query;
    }
}
