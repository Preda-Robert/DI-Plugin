using System;
using API.Interfaces;
using API.Repositories;

namespace API.Data;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    // Add the repos that have more functionality than the generic repository here and in the interface
    // e.g. IAllergyRepository etc.
    /*
        e.g.
        public readonly IAllergyRepository AllergyRepository;
        public UnitOfWork(DataContext context, IAllergyRepository allergyRepository)
        {
            _context = context;
            AllergyRepository = allergyRepository;
        }
        Also, for every repo or service inherit the base service or base repo as well 
        as their respective interfaces
    */
    public IAllergyRepository AllergyRepository { get; }
    public IIngredientCategoryRepository IngredientCategoryRepository { get; }
    public IIngredientRepository IngredientRepository { get; }
    public IRecipeRepository RecipeRepository { get; }
    public IServingTypeRepository ServingTypeRepository { get; }
    public ICookwareRepository CookwareRepository { get; }
    public IUserRepository UserRepository { get; }
    public IMealPlanRepository MealPlanRepository { get; }
    public IRecipeInstructionRepository RecipeInstructionRepository { get; }
    public IRecipeIngredientRepository RecipeIngredientRepository { get; }
    public IRecipeCookwareRepository RecipeCookwareRepository { get; }
    public IDietaryPreferenceRepository DietaryPreferenceRepository { get; }
    private readonly DataContext _context;
    private Dictionary<Type, object> _repositories;
    private readonly IServiceProvider _serviceProvider;
    public IPhotoRepository PhotoRepository { get; }
    private bool _disposed;

    public UnitOfWork(DataContext context, IServiceProvider serviceProvider,
        IAllergyRepository allergyRepository,
        IIngredientCategoryRepository ingredientCategoryRepository,
        IIngredientRepository ingredientRepository,
        IRecipeRepository recipeRepository,
        IServingTypeRepository servingTypeRepository,
        ICookwareRepository cookwareRepository,
        IUserRepository userRepository,
        IPhotoRepository photoRepository,
        IMealPlanRepository mealPlanRepository,
        IRecipeInstructionRepository recipeInstructionRepository,
        IRecipeIngredientRepository recipeIngredientRepository,
        IRecipeCookwareRepository recipeCookwareRepository,
        IDietaryPreferenceRepository dietaryPreferenceRepository)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        AllergyRepository = allergyRepository;
        IngredientCategoryRepository = ingredientCategoryRepository;
        IngredientRepository = ingredientRepository;
        RecipeRepository = recipeRepository;
        ServingTypeRepository = servingTypeRepository;
        CookwareRepository = cookwareRepository;
        UserRepository = userRepository;
        PhotoRepository = photoRepository;
        MealPlanRepository = mealPlanRepository;
        RecipeInstructionRepository = recipeInstructionRepository;
        RecipeIngredientRepository = recipeIngredientRepository;
        RecipeCookwareRepository = recipeCookwareRepository;
        DietaryPreferenceRepository = dietaryPreferenceRepository;
        _repositories = new Dictionary<Type, object>();
    }
    public IBaseRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        var type = typeof(TEntity);

        if (!_repositories.ContainsKey(type))
        {
            var repositoryType = _serviceProvider.GetService(typeof(IBaseRepository<TEntity>));
            _repositories[type] = repositoryType!;
        }

        return (IBaseRepository<TEntity>)_repositories[type];
    }

    public async Task<bool> SaveAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _repositories?.Clear();
                _context?.Dispose();
            }

            _disposed = true;
        }
    }
}