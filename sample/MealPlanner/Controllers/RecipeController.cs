using System;
using API.DTO;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;


namespace API.Controllers;

public class RecipesController : BaseAPIController
{
    private readonly IUnitOfServices _unitOfServices;
    public RecipesController(IUnitOfServices unitOfServices)
    {
        _unitOfServices = unitOfServices;
    }
    [HttpPost]
    public async Task<ActionResult<RecipeDTO>> CreateRecipe([FromBody] RecipeDTO recipe)
    {
        var recipeServiceResult = await _unitOfServices.RecipeService.Create(recipe);
        return recipeServiceResult;
    }
    [HttpGet]
    public async Task<ActionResult<ICollection<RecipeDTO>>> GetRecipes([FromQuery] RecipeParams recipeParams)
    {
        var recipesServiceResult = await _unitOfServices.RecipeService.GetAllAsync(recipeParams);
        var recipes = recipesServiceResult.Value!;
        Response.AddPaginationHeader(recipes.CurrentPage, recipes);
        return Ok(recipes);
    }

}