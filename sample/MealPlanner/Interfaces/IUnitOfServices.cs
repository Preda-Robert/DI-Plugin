using System;
using API.Services;

namespace API.Interfaces;

public interface IUnitOfServices : IDisposable
{
    IAllergyService AllergyService { get; }
    IIngredientCategoryService IngredientCategoryService { get; }
    IIngredientService IngredientService { get; }
    IRecipeService RecipeService { get; }
    IServingTypeService ServingTypeService { get; }
    ICookwareService CookwareService { get; }
    ITokenService TokenService { get; }
    IUserService UserService { get; }
    IAuthService AuthService { get; }
    IPhotoService PhotoService { get; }
    IMealPlanService MealPlanService { get; }
    IDietaryPreferenceService DietaryPreferenceService { get; }
    IBaseService<TEntity, TDTO> Service<TEntity, TDTO>()
        where TEntity : class
        where TDTO : class;
}
