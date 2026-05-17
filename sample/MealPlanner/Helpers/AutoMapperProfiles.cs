using API.DTO;
using API.DTOs;
using API.Entities;
using API.Extensions;
using AutoMapper;

namespace API.Helpers;

public class AutoMapperProfiles: Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<Allergy, AllergyDTO>();
        CreateMap<AllergyDTO, Allergy>();

        CreateMap<Photo, PhotoDTO>();
        CreateMap<PhotoDTO, Photo>();

        CreateMap<ShoppingListItem, ShoppingListItemDTO>();
        CreateMap<ShoppingListItemDTO, ShoppingListItem>();

        CreateMap<Cookware, CookwareDTO>()
            .ForMember(d => d.PhotoUrl, o => o.MapFrom(p => p.Photo!.Url));
        CreateMap<CookwareDTO, Cookware>();

        CreateMap<ApplicationUser, UserDTO>()
            .ForMember(d => d.PhotoUrl, o => o.MapFrom(p => p.Photo!.Url));

        CreateMap<RecipeCookware, RecipeCookwareDTO>();
        CreateMap<RecipeCookwareDTO, RecipeCookware>();

        CreateMap<DietaryPreferenceDTO, DietaryPreferences>();
        CreateMap<DietaryPreferences, DietaryPreferenceDTO>();

        CreateMap<RecipeCookware, CookwareDTO>();
        CreateMap<CookwareDTO, RecipeCookware>();

        CreateMap<IngredientCategory, IngredientCategoryDTO>();
        CreateMap<IngredientCategoryDTO, IngredientCategory>();

        CreateMap<ServingType, ServingTypeDTO>();
        CreateMap<ServingTypeDTO, ServingType>();

        CreateMap<Ingredient, IngredientDTO>()
            .ForMember(d => d.PhotoUrl, o => o.MapFrom(p => p.Photo!.Url));
        CreateMap<IngredientDTO, Ingredient>();

        CreateMap<Recipe, RecipeDTO>()
            .ForMember(dest => dest.Ingredients, opt => opt.MapFrom(src => src.Ingredients))
            .ForMember(dest => dest.Cookware, opt => opt.MapFrom(src => src.Cookware.Select(rc => rc.Cookware)))
            .ForMember(dest => dest.Instructions, opt => opt.MapFrom(src => src.Instructions))
            .ForMember(dest => dest.ServingType, opt => opt.MapFrom(src => src.ServingType));

        CreateMap<RecipeDTO, Recipe>()
            .ForMember(dest => dest.Ingredients, opt => opt.Ignore()) 
            .ForMember(dest => dest.Cookware, opt => opt.Ignore())
            .ForMember(dest => dest.Instructions, opt => opt.Ignore())
            .ForMember(dest => dest.Allergies, opt => opt.Ignore())
            .ForMember(dest => dest.ServingType, opt => opt.MapFrom(src => src.ServingType));

        CreateMap<RecipeIngredient, RecipeIngredientDTO>();
        CreateMap<RecipeIngredientDTO, RecipeIngredient>();

        CreateMap<RecipeInstruction, RecipeInstructionDTO>();
        CreateMap<RecipeInstructionDTO, RecipeInstruction>();

        CreateMap<Ingredient, RecipeIngredientDTO>();
        CreateMap<RecipeIngredientDTO, Ingredient>();

        CreateMap<RegisterDTO, ApplicationUser>();

        CreateMap<DietType, DietTypeDTO>();
        CreateMap<DietTypeDTO, DietType>();

        CreateMap<MealPlan, MealPlanDTO>()
            .ForMember(d => d.PhotoUrl, o => o.MapFrom(p => p.Photo!.Url));
        CreateMap<MealPlanDTO, MealPlan>();
        CreateMap<MealPlanRecipe, MealPlanRecipeDTO>();
            
        CreateMap<MealPlanRecipe, MealPlanRecipeDTO>();
        CreateMap<MealPlanRecipeDTO, MealPlanRecipe>();
    
        CreateMap<MemberDTO, ApplicationUser>();
        CreateMap<ApplicationUser, MemberDTO>();

        CreateMap<MemberUpdateDTO, ApplicationUser>();

        CreateMap<RecipeUpdateDTO, Recipe>();
    }
}
