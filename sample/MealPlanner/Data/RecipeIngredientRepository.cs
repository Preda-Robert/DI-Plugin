using System;
using API.Entities;
using API.Interfaces;
using API.Repositories;

namespace API.Data;

public class RecipeIngredientRepository : BaseRepository<RecipeIngredient>, IRecipeIngredientRepository
{

    public RecipeIngredientRepository(DataContext context) : base(context)
    {
    }

}
