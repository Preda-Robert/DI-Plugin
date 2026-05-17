using System;
using API.Entities;
using API.Helpers;

namespace API.Interfaces;

public interface IIngredientRepository : IBaseRepository<Ingredient>
{
    Task<Ingredient?> GetIngredientByNameAsync(string name);
    IQueryable<Ingredient> GetIngredients(IngredientParams ingredientParams);
}
