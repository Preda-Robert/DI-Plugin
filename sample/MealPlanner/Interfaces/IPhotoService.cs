using API.DTO;
using API.Entities;
using CloudinaryDotNet.Actions;

namespace API.Interfaces;

public interface IPhotoService : IBaseService<Photo, PhotoDTO>
{
    Task<ImageUploadResult> AddPhotoAsync(IFormFile file);
    Task<DeletionResult> DeletePhotoAsync(string publicId);
}
