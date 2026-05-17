using System;
using API.DTO;
using API.Entities;
using API.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace API.Interfaces;

public interface ICookwareService : IBaseService<Cookware, CookwareDTO>
{
    Task<ActionResult<PagedList<CookwareDTO>>> GetAllAsync(CookwareParams cookwareParams);
}
