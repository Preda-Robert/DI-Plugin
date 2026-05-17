using System;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class CookwareRepository : BaseRepository<Cookware>, ICookwareRepository
{
    public CookwareRepository(DataContext context) : base(context)
    {
    }

    public IQueryable<Cookware> GetCookwares(CookwareParams cookwareParams)
    {
        IQueryable<Cookware> query = _context.Cookwares.AsQueryable();
        if (!string.IsNullOrEmpty(cookwareParams.SearchTerm))
        {
            query = query.Where(a => a.Name.ToLower().Contains(cookwareParams.SearchTerm.ToLower()));
        }
        query = query.OrderBy(a => a.Name);
        return query;

    }

    public async Task<Cookware?> GetCookwareByNameAsync(string name)
    {
        return await _context.Cookwares
            .FirstOrDefaultAsync(c => c.Name == name);
    }
}
