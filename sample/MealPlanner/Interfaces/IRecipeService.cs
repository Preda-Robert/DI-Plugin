using System;
using API.Controllers;
using API.DTO;
using API.Entities;
using API.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace API.Interfaces;

public interface IRecipeService : IBaseService<Recipe,RecipeDTO>
{
    Task<ActionResult<PagedList<RecipeDTO>>> GetAllAsync(RecipeParams recipeParams);
}
