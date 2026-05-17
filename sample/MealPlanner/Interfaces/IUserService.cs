using System;
using API.DTO;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Services;

public interface IUserService : IBaseService<ApplicationUser, UserDTO>
{
    Task<ActionResult> UpdateUserAsync(int id, MemberUpdateDTO memberUpdateDTO);
    Task<ActionResult<MemberDTO>> GetUserByUsernameAsync(string username, bool isCurrentUser = false);
    Task<ActionResult<MemberDTO>> GetUserByIdAsync(int id, bool isCurrentUser = false);
    Task<ActionResult<PhotoDTO>> AddPhotoAsync(string username, IFormFile file);
    Task<ActionResult<bool>> DeletePhotoAsync(string username);
}
