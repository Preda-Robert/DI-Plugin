using System;
using API.Data;
using API.DTO;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace API.Services;

public class AllergyService(IUnitOfWork _unitOfWork, IMapper _mapper) : BaseService<Allergy, AllergyDTO>(_unitOfWork, _mapper), IAllergyService
{
    public async Task<ActionResult<PagedList<AllergyDTO>>> GetAllAsync(AllergyParams allergyParams)
    {
        var allergyQuery = _unitOfWork.AllergyRepository.GetAllergiesAsync(allergyParams);
        return await PagedList<AllergyDTO>.CreateAsync(allergyQuery.ProjectTo<AllergyDTO>(_mapper.ConfigurationProvider), allergyParams.PageNumber, allergyParams.PageSize);
    }
}
