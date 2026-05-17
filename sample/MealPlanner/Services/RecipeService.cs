using System;
using API.Controllers;
using API.DTO;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Humanizer;
using Microsoft.AspNetCore.Mvc;

namespace API.Services;

public class RecipeService : BaseService<Recipe, RecipeDTO>, IRecipeService
{
    private IIngredientService _ingredientService;
    private IIngredientCategoryService _ingredientCategoryService;
    public RecipeService(IIngredientService ingredientService, IIngredientCategoryService ingredientCategoryService, IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper)
    {
        _ingredientService = ingredientService;
        _ingredientCategoryService = ingredientCategoryService;
    }

    public override async Task<ActionResult<RecipeDTO>> Create(RecipeDTO recipe)
    {
        string logPath = "logs/recipe_seeding.log";
        Directory.CreateDirectory("logs");

        void Log(string message)
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] {message}{Environment.NewLine}");
        }

        try
        {
            Log($"Attempting to create recipe: {recipe.Name}");

            Recipe? existingRecipe = await _unitOfWork.RecipeRepository.GetRecipeByNameAsync(recipe.Name);
            if (existingRecipe != null)
            {
                Log($"Recipe '{recipe.Name}' already exists. Skipping.");
                return new BadRequestObjectResult($"Recipe with name {recipe.Name} already exists.");
            }

            var recipeEntity = new Recipe
            {
                Name = recipe.Name,
                Description = recipe.Description
            };

            recipeEntity.ServingType = _mapper.Map<ServingType>(recipe.ServingType);
            recipeEntity.Difficulty = recipe.Difficulty;
            recipeEntity.CookingTime = recipe.CookingTime;

            await _unitOfWork.RecipeRepository.AddAsync(recipeEntity);
            await _unitOfWork.SaveAsync();
            List<Allergy> allergies = [];
            Log($"Created base recipe entity: {recipe.Name}");

            foreach (var ingredientRecipe in recipe.Ingredients)
            {
                var ingredient = ingredientRecipe.Ingredient;
                Log($"Processing ingredient: {ingredient.Name}");

                var ingredientEntity = await _unitOfWork.IngredientRepository.GetIngredientByNameAsync(ingredient.Name);
                if (ingredientEntity == null)
                {
                    var ingredientCategory = await _unitOfWork.IngredientCategoryRepository.GetCategoryByNameAsync(ingredient.Category.Name);
                    if (ingredientCategory == null)
                    {
                        Log($"Ingredient category '{ingredient.Category.Name}' does not exist. Creating new category.");
                        await _ingredientCategoryService.Create(ingredient.Category);
                    }
                    await _ingredientService.Create(ingredient);
                    ingredientEntity = await _unitOfWork.IngredientRepository.GetIngredientByNameAsync(ingredient.Name);
                }
                if (ingredientEntity == null)
                {
                    Log($"Failed to create ingredient: {ingredient.Name}");
                    return new BadRequestObjectResult($"Ingredient with name {ingredient.Name} does not exist.");
                }
                Log($"Ingredient '{ingredient.Name}' created successfully.");
                if(ingredientEntity.Allergy != null)
                {
                    Log($"Ingredient '{ingredientEntity.Name}' has allergy: {ingredientEntity.Allergy.Name}");
                    var allergyEntity = await _unitOfWork.AllergyRepository.GetAllergyByNameAsync(ingredientEntity.Allergy.Name);
                    allergies.Add(allergyEntity!);
                }
                else
                {
                    Log($"Ingredient '{ingredient.Name}' has no allergy.");
                }
                var recipeIngredient = new RecipeIngredient
                {
                    Ingredient = ingredientEntity,
                    Recipe = recipeEntity,
                    Quantity = ingredientRecipe.Quantity,
                    IngredientId = ingredientEntity.Id,
                    RecipeId = recipeEntity.Id
                };
                recipeIngredient = await _unitOfWork.RecipeIngredientRepository.AddAsync(recipeIngredient);
                ingredientEntity.RecipeIngredients.Add(recipeIngredient);
                recipeEntity.Ingredients.Add(recipeIngredient);
                await _unitOfWork.SaveAsync();
                Log($"Added ingredient to recipe: {ingredient.Name}");
            }

            foreach (var cookwareRecipe in recipe.Cookware)
            {
                Log($"Processing cookware: {cookwareRecipe.Name}");

                var cookwareEntity = await _unitOfWork.CookwareRepository.GetCookwareByNameAsync(cookwareRecipe.Name);
                if (cookwareEntity == null)
                {
                    Log($"Failed to create cookware: {cookwareRecipe.Name}");
                    return new BadRequestObjectResult($"Cookware with name {cookwareRecipe.Name} does not exist.");
                }

                var recipeCookware = new RecipeCookware
                {
                    Recipe = recipeEntity,
                    Cookware = cookwareEntity,
                    RecipeId = recipeEntity.Id,
                    CookwareId = cookwareEntity.Id
                };

                recipeCookware = await _unitOfWork.RecipeCookwareRepository.AddAsync(recipeCookware);
                cookwareEntity.Recipes.Add(recipeCookware);
                recipeEntity.Cookware.Add(recipeCookware);
                await _unitOfWork.SaveAsync();
                Log($"Added cookware to recipe: {cookwareRecipe.Name}");
            }

            foreach (var instruction in recipe.Instructions)
            {
                var recipeInstruction = new RecipeInstruction
                {
                    Description = instruction.Description,
                    InstructionIndex = instruction.InstructionIndex,
                    RecipeId = recipeEntity.Id,
                    Recipe = recipeEntity
                };
                recipeInstruction = await _unitOfWork.RecipeInstructionRepository.AddAsync(recipeInstruction);
                recipeEntity.Instructions.Add(recipeInstruction);
                await _unitOfWork.SaveAsync();
                Log($"Added instruction step {instruction.InstructionIndex} to recipe: {recipe.Name}");
            }

            recipeEntity.Allergies = allergies;

            Log($"Finalizing recipe: {recipe.Name}");
            Log($"Recipe '{recipe.Name}' has {recipeEntity.Ingredients.Count} ingredients, {recipeEntity.Cookware.Count} cookware items, and {recipeEntity.Instructions.Count} instructions.");
            await _unitOfWork.SaveAsync();

            Log($"Successfully created recipe: {recipe.Name}");

            var recipeDTO = _mapper.Map<RecipeDTO>(recipeEntity);
            var allergyEntities = await _unitOfWork.AllergyRepository.GetAllAsync();
            return recipeDTO;
        }
        catch (Exception ex)
        {
            Log($"Exception while creating recipe '{recipe.Name}': {ex.Message}");
            throw;
        }
    }


    public async Task<ActionResult<RecipeDTO>> CreateUserRecipe(string username, RecipeDTO recipe)
    {
        return new OkObjectResult(recipe);
    }

    public async Task<ActionResult<PagedList<RecipeDTO>>> GetAllAsync(RecipeParams recipeParams)
    {
        var recipeQuery = _unitOfWork.RecipeRepository.GetRecipes(recipeParams);
        var pagedRecipeList = await PagedList<RecipeDTO>.CreateAsync(recipeQuery.ProjectTo<RecipeDTO>(_mapper.ConfigurationProvider), recipeParams.PageNumber, recipeParams.PageSize);
        return pagedRecipeList;
    }
}
