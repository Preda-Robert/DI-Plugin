using System;
using API.DTO;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class DietaryPreferenceController : BaseAPIController
{
    private readonly IUnitOfServices _unitOfServices;
    public DietaryPreferenceController(IUnitOfServices unitOfServices)
    {
        _unitOfServices = unitOfServices;
    }

    [HttpPost]
    public async Task<ActionResult<DietaryPreferenceDTO>> CreateDietaryPreference(DietaryPreferenceDTO dietaryPreferenceDTO)
    {
        var dietaryPreferenceServiceResult = await _unitOfServices.DietaryPreferenceService.Create(dietaryPreferenceDTO);
        return dietaryPreferenceServiceResult;
    }

    [HttpPost("save")]
    public async Task<ActionResult> SaveDietaryPreference(SaveDietPreferenceDTO saveDietPreferenceDTO)
    {
        var dietaryPreferenceServiceResult = await _unitOfServices.DietaryPreferenceService.SaveDietaryPreference(saveDietPreferenceDTO);
        return dietaryPreferenceServiceResult;
    }

    [HttpGet]
    public async Task<ActionResult<ICollection<DietaryPreferenceDTO>>> GetDietaryPreferences()
    {
        var dietaryPreferencesServiceResult = await _unitOfServices.DietaryPreferenceService.GetAllAsync();
        return dietaryPreferencesServiceResult;
    }
}
