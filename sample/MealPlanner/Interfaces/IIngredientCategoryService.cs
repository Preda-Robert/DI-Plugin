using System;
using API.DTO;
using API.Entities;

namespace API.Interfaces;

public interface IIngredientCategoryService : IBaseService<IngredientCategory, IngredientCategoryDTO>
{
    Task<IngredientCategoryDTO> GetCategoryByNameAsync(string name);
    Task<IngredientCategoryDTO> GetCategoryWithIngredientsAsync(int id);
    Task<IngredientCategoryDTO> GetCategoryWithIngredientsByNameAsync(string name);
    Task<bool> CategoryExistsAsync(string name);
    Task<bool> CategoryExistsAsync(int id);
}
