using System;
using API.DTO;
using API.Entities;
using Microsoft.AspNetCore.Mvc;

namespace API.Interfaces;

public interface IDietaryPreferenceService : IBaseService<DietaryPreferences, DietaryPreferenceDTO>
{
    Task<ActionResult> SaveDietaryPreference(SaveDietPreferenceDTO saveDietPreferenceDTO);
}