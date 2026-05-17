using System;
using API.Entities;
using API.Helpers;

namespace API.Interfaces;

public interface ICookwareRepository : IBaseRepository<Cookware>
{
    public IQueryable<Cookware> GetCookwares(CookwareParams cookwareParams);

    public Task<Cookware?> GetCookwareByNameAsync(string name);
}
