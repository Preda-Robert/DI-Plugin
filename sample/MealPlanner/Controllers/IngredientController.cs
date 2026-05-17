using System;
using API.DTO;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class IngredientsController : BaseAPIController
{
    private readonly IUnitOfServices _unitOfServices;
    public IngredientsController(IUnitOfServices unitOfServices)
    {
        _unitOfServices = unitOfServices;
    }
    [HttpPost]
    public async Task<ActionResult<IngredientDTO>> CreateIngredient([FromBody] IngredientDTO ingredient)
    {
        var ingredientServiceResult = await _unitOfServices.IngredientService.Create(ingredient);
        return ingredientServiceResult;
    }

    [HttpGet]
    public async Task<ActionResult<ICollection<IngredientDTO>>> GetIngredients([FromQuery] IngredientParams ingredientParams)
    {
        ingredientParams.CurrentUser = User.GetUserId();
        var ingredientServiceResult = await _unitOfServices.IngredientService.GetAllAsync(ingredientParams);
        var ingredients = ingredientServiceResult.Value!;
        Response.AddPaginationHeader(ingredients.CurrentPage, ingredients);
        return Ok(ingredients);
    }
}
