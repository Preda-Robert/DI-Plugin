using System;

namespace API.Data;

using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using API.Repositories;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

public class PhotoRepository : BaseRepository<Photo>, IPhotoRepository
{
    public PhotoRepository(DataContext context) : base(context)
    {
    }
    public async Task<Photo?> GetPhotoById(int id)
    {
        return await _context.Photos.IgnoreQueryFilters().SingleOrDefaultAsync(p => p.Id == id);
    }

    public void RemovePhoto(Photo photo)
    {
        _context.Photos.Remove(photo);
    }
}
