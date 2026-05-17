using System;
using API.DTO;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using API.Repositories;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class AllergyRepository : BaseRepository<Allergy>, IAllergyRepository
{
    private IMapper _mapper;
    public AllergyRepository(DataContext context, IMapper mapper) : base(context)
    {
        _mapper = mapper;
    }

    public async Task<bool> AllergyExistsAsync(string name)
    {
        return await _context.Allergies.AnyAsync(a => a.Name == name);
    }

    public Task<bool> AllergyExistsAsync(int id)
    {
        return _context.Allergies.AnyAsync(a => a.Id == id);
    }

    public async Task<ICollection<Allergy>> GetAllAllergiesAsync()
    {
        return await _context.Allergies.ToListAsync();
    }

    public async Task<ICollection<Allergy>> GetAllAllergiesByUserIdAsync(string userId)
    {
        throw new NotImplementedException();
    }
    public IQueryable<Allergy> GetAllergiesAsync(AllergyParams allergyParams)
    {
        IQueryable<Allergy> query;

        if (allergyParams.CurrentUser > -1)
        {
            query = _context.Users
                .Where(u => u.Id == allergyParams.CurrentUser)
                .SelectMany(u => u.DietaryPreferences.Allergies)
                .AsQueryable();
        }
        else
        {
            query = _context.Allergies.AsQueryable();
        }

        if (!string.IsNullOrEmpty(allergyParams.SearchTerm))
        {
            query = query.Where(a => a.Name.Contains(allergyParams.SearchTerm));
        }

        query = query.OrderBy(a => a.Name);

        return query;
    }

    public async Task<Allergy?> GetAllergyByNameAsync(string name)
    {
        return await _context.Allergies
            .FirstOrDefaultAsync(a => a.Name == name);
    }
}
