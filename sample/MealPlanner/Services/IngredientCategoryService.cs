using System;
using API.DTO;
using API.Entities;
using API.Interfaces;
using AutoMapper;

namespace API.Services;

public class IngredientCategoryService : BaseService<IngredientCategory, IngredientCategoryDTO>, IIngredientCategoryService
{

    public IngredientCategoryService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper)
    {
    }

    public async Task<IngredientCategoryDTO> GetCategoryByNameAsync(string name)
    {
        var category = await _unitOfWork.IngredientCategoryRepository.GetCategoryByNameAsync(name);
        return _mapper.Map<IngredientCategoryDTO>(category);
    }

    public async Task<IngredientCategoryDTO> GetCategoryWithIngredientsAsync(int id)
    {
        var category = await _unitOfWork.IngredientCategoryRepository.GetCategoryWithIngredientsAsync(id);
        return _mapper.Map<IngredientCategoryDTO>(category);
    }

    public async Task<IngredientCategoryDTO> GetCategoryWithIngredientsByNameAsync(string name)
    {
        var category = await _unitOfWork.IngredientCategoryRepository.GetCategoryWithIngredientsByNameAsync(name);
        return _mapper.Map<IngredientCategoryDTO>(category);
    }

    public async Task<bool> CategoryExistsAsync(string name)
    {
        return await _unitOfWork.IngredientCategoryRepository.CategoryExistsAsync(name);
    }

    public async Task<bool> CategoryExistsAsync(int id)
    {
        return await _unitOfWork.IngredientCategoryRepository.CategoryExistsAsync(id);
    }
}
