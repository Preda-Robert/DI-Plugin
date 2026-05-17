using System;
using API.DTO;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class IngredientCategoryController : BaseAPIController
{
    private readonly IUnitOfServices _unitOfServices;
    public IngredientCategoryController(IUnitOfServices unitOfServices)
    {
        _unitOfServices = unitOfServices;
    }
    [HttpPost]
    public async Task<ActionResult<IngredientCategoryDTO>> CreateIngredientCategory([FromBody] IngredientCategoryDTO ingredientCategory)
    {
        var ingredientCategoryServiceResult = await _unitOfServices.IngredientCategoryService.Create(ingredientCategory);
        return ingredientCategoryServiceResult;
    }
    [HttpGet]
    public async Task<ActionResult<ICollection<IngredientCategoryDTO>>> GetIngredientCategories()
    {
        var ingredientCategoriesServiceResult = await _unitOfServices.IngredientCategoryService.GetAllAsync();
        return ingredientCategoriesServiceResult;
    }

}