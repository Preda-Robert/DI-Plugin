using System;
using API.Entities;

namespace API.Interfaces;

public interface IDietaryPreferenceRepository : IBaseRepository<DietaryPreferences>
{
    Task<DietaryPreferences?> GetDietaryPreferenceByUserId(int userId);
}
