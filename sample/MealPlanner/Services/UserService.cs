using System;
using API.DTO;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Services;

public class UserService : BaseService<ApplicationUser, UserDTO>, IUserService
{
    private IPhotoService _photoService;
    public UserService(IPhotoService photoService, IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper)
    {
        _photoService = photoService;
    }

    public async Task<ActionResult> UpdateUserAsync(int id, MemberUpdateDTO memberUpdateDTO)
    {
        var user = await _unitOfWork.UserRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            return new BadRequestObjectResult("User not found");
        }
        _mapper.Map(memberUpdateDTO, user);
        _unitOfWork.UserRepository.Update(user);
        var result = await _unitOfWork.SaveAsync();
        if (!result)
        {
            return new BadRequestObjectResult("Failed to update user");
        }
        return new NoContentResult();
    }

    public async Task<ActionResult<PhotoDTO>> AddPhotoAsync(string username, IFormFile file)
    {
        var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
        if (user == null)
        {
            return new BadRequestObjectResult("User not found");
        }
        var photoUploadResult = await _photoService.AddPhotoAsync(file);
        if (photoUploadResult.Error != null)
        {
            return new BadRequestObjectResult(photoUploadResult.Error.Message);
        }
        var photo = new Photo
        {
            Url = photoUploadResult.SecureUrl.AbsoluteUri,
            PublicId = photoUploadResult.PublicId
        };
        user.Photo = photo;
        await _unitOfWork.UserRepository.UpdateAsync(user);
        var result = await _unitOfWork.SaveAsync();
        if (!result)
        {
            await _photoService.DeletePhotoAsync(photo.PublicId);
            return new BadRequestObjectResult("Failed to add photo");
        }
        var photoDTO = _mapper.Map<PhotoDTO>(photo);
        return photoDTO;

    }

    public async Task<ActionResult<bool>> DeletePhotoAsync(string username)
    {
        var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
        if (user == null)
        {
            return new BadRequestObjectResult("User not found");
        }
        var photo = user.Photo;
        if (photo == null)
        {
            return new BadRequestObjectResult("No photo to delete");
        }
        if (photo.PublicId != null)
        {
            var deletionResult = await _photoService.DeletePhotoAsync(photo.PublicId);
            if (deletionResult.Error != null)
            {
                return new BadRequestObjectResult(deletionResult.Error.Message);
            }
        }
        _unitOfWork.PhotoRepository.Delete(photo);
        user.Photo = null;
        await _unitOfWork.UserRepository.UpdateAsync(user);
        var result = await _unitOfWork.SaveAsync();
        if (!result)
        {
            return new BadRequestObjectResult("Failed to delete photo");
        }
        return true;
    }

    public async Task<ActionResult<MemberDTO>> GetUserByIdAsync(int id, bool isCurrentUser = false)
    {
        var user = await _unitOfWork.UserRepository.GetUserByIdAsync(id, isCurrentUser);
        if(user == null)
        {
            return new BadRequestObjectResult("User not found");
        }
        var memberDTO = _mapper.Map<MemberDTO>(user);
        return memberDTO;
    }

    public async Task<ActionResult<MemberDTO>> GetUserByUsernameAsync(string username, bool isCurrentUser = false)
    {
        var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username, isCurrentUser);
        if(user == null)
        {
            return new BadRequestObjectResult("User not found");
        }
        var memberDTO = _mapper.Map<MemberDTO>(user);
        return memberDTO;
    }
}
