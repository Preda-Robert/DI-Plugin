using System;
using API.DTO;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace API.Services;

public class DietaryPreferenceService : BaseService<DietaryPreferences, DietaryPreferenceDTO>, IDietaryPreferenceService
{
    public DietaryPreferenceService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper)
    {
    }

    public async Task<ActionResult> SaveDietaryPreference(SaveDietPreferenceDTO saveDietPreferenceDTO)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(saveDietPreferenceDTO.UserId);
        if (user == null)
        {
            return new NotFoundObjectResult("User not found");
        }
        
        // First check if there's an existing preference and delete it
        var existingPreference = await _unitOfWork.DietaryPreferenceRepository.GetDietaryPreferenceByUserId(saveDietPreferenceDTO.UserId);
        if (existingPreference != null)
        {
            _unitOfWork.DietaryPreferenceRepository.Delete(existingPreference);
            await _unitOfWork.SaveAsync(); // Save changes to database before adding new entity
        }
        
        // Create new dietary preference
        var dietType = await _unitOfWork.Repository<DietType>().GetByIdAsync(saveDietPreferenceDTO.DietTypeId);
        var servingType = await _unitOfWork.ServingTypeRepository.GetByIdAsync(saveDietPreferenceDTO.ServingTypeId);
        
        if (dietType == null)
        {
            return new NotFoundObjectResult("Diet type not found");
        }
        
        if (servingType == null)
        {
            return new NotFoundObjectResult("Serving type not found");
        }
        
        var dietaryPreference = new DietaryPreferences
        {
            UserId = saveDietPreferenceDTO.UserId,
            DietTypeId = saveDietPreferenceDTO.DietTypeId,
            ServingTypeId = saveDietPreferenceDTO.ServingTypeId,
            // Don't assign navigation properties here - EF will handle relationships
            Allergies = new List<Allergy>()
        };
        
        // Add allergies
        foreach (var allergyId in saveDietPreferenceDTO.AllergyIds)
        {
            var allergyEntity = await _unitOfWork.AllergyRepository.GetByIdAsync(allergyId);
            if (allergyEntity != null)
            {
                dietaryPreference.Allergies.Add(allergyEntity);
            }
        }
        
        // Update user setup status
        user.HasDoneSetup = true;
        await _unitOfWork.UserRepository.UpdateAsync(user);
        
        // Add new dietary preference - DON'T call Update on it
        await _unitOfWork.DietaryPreferenceRepository.AddAsync(dietaryPreference);
        
        // Save all changes at once
        await _unitOfWork.SaveAsync();
        
        // Return the mapped result
        return new OkObjectResult(_mapper.Map<DietaryPreferenceDTO>(dietaryPreference));
    }
}