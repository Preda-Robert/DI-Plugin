using System;
using API.DTO;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/servingtypes")]
[ApiController]
public class ServingTypesController : BaseAPIController
{
    private readonly IUnitOfServices _unitOfServices;
    public ServingTypesController(IUnitOfServices unitOfServices)
    {
        _unitOfServices = unitOfServices;
    }
    [HttpPost]
    public async Task<ActionResult<ServingTypeDTO>> CreateServingType([FromBody] ServingTypeDTO servingType)
    {
        var servingTypeServiceResult = await _unitOfServices.ServingTypeService.Create(servingType);
        return servingTypeServiceResult;
    }
    [HttpGet]
    public async Task<ActionResult<ICollection<ServingTypeDTO>>> GetServingTypes()
    {
        var servingTypesServiceResult = await _unitOfServices.ServingTypeService.GetAllAsync();
        return servingTypesServiceResult;
    }
}
