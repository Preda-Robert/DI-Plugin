using System;
using API.DTO;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using API.Extensions;

namespace API.Controllers;

public class UsersController : BaseAPIController
{
    private readonly IUnitOfServices _unitOfServices;

    public UsersController(IUnitOfServices unitOfServices)
    {
        _unitOfServices = unitOfServices;
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<MemberDTO>> GetUser(string username)
    {
        var member = await _unitOfServices.UserService.GetUserByUsernameAsync(username);
        return member;
    }

    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateDTO memberUpdateDTO)
    {
        var result = await _unitOfServices.UserService.UpdateUserAsync(User.GetUserId(), memberUpdateDTO);
        return result;
    }

    [HttpPost("add-photo")]
    public async Task<ActionResult<PhotoDTO>> AddPhoto(IFormFile file)
    {
        var user = await _unitOfServices.UserService.AddPhotoAsync(User.GetUsername(), file);
        return user;
    }

    [HttpDelete("delete-photo")]
    public async Task<ActionResult<bool>> DeletePhoto()
    {
        var result = await _unitOfServices.UserService.DeletePhotoAsync(User.GetUsername());
        return result;
    }

}
