using System;
using System.Threading.Tasks;

namespace API.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IAllergyRepository AllergyRepository { get; }
        IIngredientCategoryRepository IngredientCategoryRepository { get; }
        IIngredientRepository IngredientRepository { get; }
        IRecipeRepository RecipeRepository { get; }
        IServingTypeRepository ServingTypeRepository { get; }
        ICookwareRepository CookwareRepository { get; }
        IUserRepository UserRepository { get; }
        IPhotoRepository PhotoRepository { get; }
        IMealPlanRepository MealPlanRepository { get; }
        IRecipeInstructionRepository RecipeInstructionRepository { get; }
        IRecipeIngredientRepository RecipeIngredientRepository { get; }
        IRecipeCookwareRepository RecipeCookwareRepository { get; }
        IDietaryPreferenceRepository DietaryPreferenceRepository { get; }

        IBaseRepository<TEntity> Repository<TEntity>() where TEntity : class;
        Task<bool> SaveAsync();
    }
}