using System;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class IngredientRepository : BaseRepository<Ingredient>, IIngredientRepository
{
    public IngredientRepository(DataContext context) : base(context)
    {
    }

    public async Task<Ingredient?> GetIngredientByNameAsync(string name)
    {
        return await _context.Ingredients
            .Include(i => i.Category)
            .Include(i => i.Allergy)
            .FirstOrDefaultAsync(i => i.Name == name);
    }

    public IQueryable<Ingredient> GetIngredients(IngredientParams ingredientParams)
    {
        var query = _context.Ingredients.AsQueryable();

        if (ingredientParams.AllergyIds != null && ingredientParams.AllergyIds.Count != 0)
        {
            query = query.Where(i => i.AllergyId.HasValue && ingredientParams.AllergyIds.Contains(i.AllergyId.Value));
        }

        if (!string.IsNullOrEmpty(ingredientParams.SearchTerm))
        {
            query = query.Where(i => i.Name.ToLower().Contains(ingredientParams.SearchTerm.ToLower()));
        }

        query = query.OrderBy(i => i.Name);
        query = query
            .Include(i => i.Category)
            .Include(i => i.Allergy);

        return query;
    }
}
