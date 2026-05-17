using System;
using API.Entities;
using API.Interfaces;
using API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class IngredientCategoryRepository : BaseRepository<IngredientCategory>, IIngredientCategoryRepository
{


    public IngredientCategoryRepository(DataContext context) : base(context)
    {

    }

    public async Task<IngredientCategory?> GetCategoryByNameAsync(string name)
    {
        return await _context.IngredientCategories
            .Include(c => c.Ingredients)
            .FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<IngredientCategory?> GetCategoryWithIngredientsAsync(int id)
    {
        return await _context.IngredientCategories
            .Include(c => c.Ingredients)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IngredientCategory?> GetCategoryWithIngredientsByNameAsync(string name)
    {
        return await _context.IngredientCategories
            .Include(c => c.Ingredients)
            .FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<bool> CategoryExistsAsync(string name)
    {
        return await _context.IngredientCategories
            .AnyAsync(c => c.Name == name);
    }

    public async Task<bool> CategoryExistsAsync(int id)
    {
        return await _context.IngredientCategories
            .AnyAsync(c => c.Id == id);
    }

    public async Task<int> GetCategoryIdByNameAsync(string name)
    {
        var category = await _context.IngredientCategories
            .FirstOrDefaultAsync(c => c.Name == name);

        return category?.Id ?? 0;
    }

}
