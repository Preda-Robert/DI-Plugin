using System;
using API.Entities;

namespace API.Interfaces;

public interface IIngredientCategoryRepository : IBaseRepository<IngredientCategory>
{
    Task<IngredientCategory?> GetCategoryByNameAsync(string name);
    Task<IngredientCategory?> GetCategoryWithIngredientsAsync(int id);
    Task<IngredientCategory?> GetCategoryWithIngredientsByNameAsync(string name);
    Task<int> GetCategoryIdByNameAsync(string name);
    Task<bool> CategoryExistsAsync(string name);
    Task<bool> CategoryExistsAsync(int id);
}
