using System;
using API.DTO;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;

namespace API.Services;

public class MealPlanService : BaseService<MealPlan, MealPlanDTO>, IMealPlanService
{

    public MealPlanService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper)
    {
    }

    public async Task<ActionResult<PagedList<MealPlanDTO>>> GetAllAsync(MealPlanParams mealPlanParams)
    {
        var mealPlansQuery = _unitOfWork.MealPlanRepository.GetMealPlans(mealPlanParams);
        return await PagedList<MealPlanDTO>.CreateAsync(mealPlansQuery.ProjectTo<MealPlanDTO>(_mapper.ConfigurationProvider), mealPlanParams.PageNumber, mealPlanParams.PageSize);
    }
}
