using System;
using API.Entities;
using API.Interfaces;
using API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class DietaryPreferenceRepository : BaseRepository<DietaryPreferences>, IDietaryPreferenceRepository
{

    public DietaryPreferenceRepository(DataContext context) : base(context)
    {
    }

    public async Task<DietaryPreferences?> GetDietaryPreferenceByUserId(int userId)
    {
        var user = await _context.Users
            .Include(u => u.DietaryPreferences)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return null;
        }
        return user.DietaryPreferences;
    }
}
