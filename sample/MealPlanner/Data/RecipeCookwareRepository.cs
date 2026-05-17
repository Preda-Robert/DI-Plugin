using System;
using API.Entities;
using API.Interfaces;
using API.Repositories;

namespace API.Data;

public class RecipeCookwareRepository : BaseRepository<RecipeCookware>, IRecipeCookwareRepository
{
    public RecipeCookwareRepository(DataContext context) : base(context)
    {
    }
}
