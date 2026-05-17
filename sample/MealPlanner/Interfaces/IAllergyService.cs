using System;
using API.DTO;
using API.Entities;
using API.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace API.Interfaces;

public interface IAllergyService : IBaseService<Allergy, AllergyDTO>
{
    Task<ActionResult<PagedList<AllergyDTO>>> GetAllAsync(AllergyParams allergyParams);
}
