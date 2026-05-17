using System;
using API.DTO;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;

namespace API.Services;

public class CookwareService : BaseService<Cookware, CookwareDTO>, ICookwareService
{
    public CookwareService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper)
    {
    }

    public async Task<ActionResult<PagedList<CookwareDTO>>> GetAllAsync(CookwareParams cookwareParams)
    {
        var cookwareQuery = _unitOfWork.CookwareRepository.GetCookwares(cookwareParams);
        return await PagedList<CookwareDTO>.CreateAsync(cookwareQuery.ProjectTo<CookwareDTO>(_mapper.ConfigurationProvider), cookwareParams.PageNumber, cookwareParams.PageSize);
    }
}
