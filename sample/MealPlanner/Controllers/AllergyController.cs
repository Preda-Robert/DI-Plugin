using System.Threading.Tasks;
using API.DTO;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class AllergiesController : BaseAPIController
    {
        private readonly IUnitOfServices _unitOfServices;

        public AllergiesController(IUnitOfServices unitOfServices)
        {
            _unitOfServices = unitOfServices;
        }

        [HttpPost]
        public async Task<ActionResult<AllergyDTO>> CreateAllergy(AllergyDTO allergyDTO)
        {
            var allergyServiceResult = await _unitOfServices.AllergyService.Create(allergyDTO);
            return allergyServiceResult;
        }

        [HttpGet]
        public async Task<ActionResult<ICollection<AllergyDTO>>> GetAllergies([FromQuery] AllergyParams allergyParams)
        {
            allergyParams.CurrentUser = User.GetUserId();
            var allergiesServiceResult = await _unitOfServices.AllergyService.GetAllAsync(allergyParams);
            var allergies = allergiesServiceResult.Value!;
            Response.AddPaginationHeader(allergies.CurrentPage, allergies);
            return Ok(allergies);
        }
    }
}
