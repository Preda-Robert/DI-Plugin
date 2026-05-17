using System;
using API.DTO;
using API.Entities;
using API.Helpers;
using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Interfaces;

public interface IIngredientService : IBaseService<Ingredient, IngredientDTO>
{
    Task<ActionResult<PagedList<IngredientDTO>>> GetAllAsync(IngredientParams ingredientParams);
    
}
