using System;
using API.Entities;
using API.Helpers;

namespace API.Interfaces;

public interface IMealPlanRepository : IBaseRepository<MealPlan>
{
    IQueryable<MealPlan> GetMealPlans(MealPlanParams mealPlanParams);
}
