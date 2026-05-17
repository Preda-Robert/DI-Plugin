using System;
using API.DTO;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class MealPlansController : BaseAPIController
{
    private readonly IUnitOfServices _unitOfServices;
    public MealPlansController(IUnitOfServices unitOfServices)
    {
        _unitOfServices = unitOfServices;
    }

    [HttpPost]
    public async Task<ActionResult<MealPlanDTO>> CreateMealPlan([FromBody] MealPlanDTO mealPlan)
    {
        var mealPlanServiceResult = await _unitOfServices.MealPlanService.Create(mealPlan);
        return mealPlanServiceResult;
    }

    [HttpGet]
    public async Task<ActionResult<ICollection<MealPlanDTO>>> GetMealPlans([FromQuery] MealPlanParams mealPlanParams)
    {
        mealPlanParams.CurrentUser = User.GetUserId();
        var mealPlanServiceResult = await _unitOfServices.MealPlanService.GetAllAsync(mealPlanParams);
        var mealPlans = mealPlanServiceResult.Value!;
        Response.AddPaginationHeader(mealPlans.CurrentPage, mealPlans);
        return Ok(mealPlans);
    }

    
}
