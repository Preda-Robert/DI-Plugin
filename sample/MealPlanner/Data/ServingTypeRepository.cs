using System;
using API.Entities;
using API.Interfaces;
using API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class ServingTypeRepository : BaseRepository<ServingType>, IServingTypeRepository
{
    public ServingTypeRepository(DataContext context) : base(context)
    {
    }

    public override async Task<ICollection<ServingType>> GetAllAsync()
    {
        // only return the first 10 records
        return await _dbSet.Take(10).ToListAsync();
    }
}
