using System;
using API.DTO;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class CookwaresController : BaseAPIController
{
    private readonly IUnitOfServices _unitOfServices;

    public CookwaresController(IUnitOfServices unitOfServices)
    {
        _unitOfServices = unitOfServices;
    }

    [HttpPost]
    public async Task<ActionResult<CookwareDTO>> CreateCookware([FromBody] CookwareDTO cookwareDTO)
    {
        var cookwareCreationResult = await _unitOfServices.CookwareService.Create(cookwareDTO);
        return cookwareCreationResult;
    }

    [HttpGet]
    public async Task<ActionResult<ICollection<CookwareDTO>>> GetCookwares([FromQuery] CookwareParams cookwareParams)
    {
        cookwareParams.CurrentUser = User.GetUserId();
        var cookwaresServiceResult = await _unitOfServices.CookwareService.GetAllAsync(cookwareParams);
        var cookwares = cookwaresServiceResult.Value!;
        Response.AddPaginationHeader(cookwares.CurrentPage, cookwares);
        return Ok(cookwares);
    }
}
