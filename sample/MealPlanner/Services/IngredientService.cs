using System;
using API.DTO;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace API.Services;

public class IngredientService : BaseService<Ingredient, IngredientDTO>, IIngredientService
{
    public IngredientService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper)
    {
    }

    public override async Task<ActionResult<IngredientDTO>> Create(IngredientDTO dto)
    {
        string logPath = "logs/ingredient_seeding.log";
        Directory.CreateDirectory("logs");

        void Log(string message)
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] {message}{Environment.NewLine}");
        }

        try
        {
            Log($"Attempting to create ingredient: {dto.Name}");

            var existingIngredient = await _unitOfWork.IngredientRepository.GetIngredientByNameAsync(dto.Name);
            if (existingIngredient != null)
            {
                Log($"Ingredient '{dto.Name}' already exists. Skipping.");
                // log the allergy
                Log($"Ingredient '{existingIngredient.Name}' already exists. Allergy: '{existingIngredient.Allergy?.Name}'");
                var existingIngredientDTO1 = new IngredientDTO
                {
                    Id = existingIngredient.Id,
                    Name = existingIngredient.Name,
                    Category = new IngredientCategoryDTO
                    {
                        Id = existingIngredient.Category.Id,
                        Name = existingIngredient.Category.Name
                    },
                    Allergy = existingIngredient.Allergy != null ? new AllergyDTO
                    {
                        Id = existingIngredient.Allergy.Id,
                        Name = existingIngredient.Allergy.Name
                    } : null
                };
                return new OkObjectResult(existingIngredientDTO1);
            }

            var category = await _unitOfWork.IngredientCategoryRepository.GetCategoryByNameAsync(dto.Category.Name);
            if (category == null)
            {
                Log($"Failed to create '{dto.Name}': Category '{dto.Category.Name}' not found.");
                return new BadRequestObjectResult($"Category with name {dto.Category.Name} does not exist.");
            }

            Allergy? allergy = null;
            if (dto.Allergy != null)
            {
                allergy = await _unitOfWork.AllergyRepository.GetAllergyByNameAsync(dto.Allergy.Name);
                if (allergy == null)
                {
                    Log($"Failed to create '{dto.Name}': Allergy '{dto.Allergy.Name}' not found.");
                    return new BadRequestObjectResult($"Allergy with name {dto.Allergy.Name} does not exist.");
                }
            }

            var createdIngredient = _mapper.Map<Ingredient>(dto);
            createdIngredient.Category = category;
            createdIngredient.Allergy = allergy;

            category.Ingredients.Add(createdIngredient);
            createdIngredient.CategoryId = category.Id;
            createdIngredient.AllergyId = allergy?.Id;
            createdIngredient.IsAllergen = dto.IsAllergen;
            createdIngredient.MeasurementType = dto.MeasurementType;
            createdIngredient.Calories = dto.Calories;
            Log($"Created Ingredient '{createdIngredient.Name}'. Allergy: '{createdIngredient.Allergy?.Name}'");
            await _unitOfWork.IngredientRepository.AddAsync(createdIngredient);
            _unitOfWork.IngredientCategoryRepository.Update(category);
            await _unitOfWork.SaveAsync();

            Log($"Successfully created ingredient: {dto.Name}");

            var existingIngredientDTO = new IngredientDTO
            {
                Id = createdIngredient.Id,
                Name = createdIngredient.Name,
                Category = new IngredientCategoryDTO
                {
                    Id = createdIngredient.Category.Id,
                    Name = createdIngredient.Category.Name
                },
                Allergy = createdIngredient.Allergy != null ? new AllergyDTO
                {
                    Id = createdIngredient.Allergy.Id,
                    Name = createdIngredient.Allergy.Name
                } : null
            };

            return new OkObjectResult(existingIngredientDTO);
        }
        catch (Exception ex)
        {
            Log($"Exception while creating ingredient '{dto.Name}': {ex.Message}");
            throw; // optional: rethrow or handle as needed
        }
    }

    public async Task<ActionResult<PagedList<IngredientDTO>>> GetAllAsync(IngredientParams ingredientParams)
    {
        var allergyQuery = _unitOfWork.IngredientRepository.GetIngredients(ingredientParams);
        return await PagedList<IngredientDTO>.CreateAsync(allergyQuery.ProjectTo<IngredientDTO>(_mapper.ConfigurationProvider), ingredientParams.PageNumber, ingredientParams.PageSize);
    }
}
