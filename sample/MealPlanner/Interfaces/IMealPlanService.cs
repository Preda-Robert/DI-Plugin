using System;
using API.DTO;
using API.Entities;
using API.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace API.Interfaces;

public interface IMealPlanService : IBaseService<MealPlan, MealPlanDTO>
{
    Task<ActionResult<PagedList<MealPlanDTO>>> GetAllAsync(MealPlanParams mealPlanParams);
}
