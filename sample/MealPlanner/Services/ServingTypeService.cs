using System;
using API.DTO;
using API.Entities;
using API.Interfaces;
using AutoMapper;

namespace API.Services;

public class ServingTypeService : BaseService<ServingType, ServingTypeDTO>, IServingTypeService
{
    public ServingTypeService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper)
    {
    }
}
