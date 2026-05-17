using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace API.Services
{
    public class BaseService<TEntity, TDTO> : IBaseService<TEntity, TDTO> 
        where TEntity : class 
        where TDTO : class
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IMapper _mapper;

        public BaseService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public virtual async Task<ActionResult<TDTO>> Create(TDTO dto)
        {

            var entity = _mapper.Map<TEntity>(dto);

            await _unitOfWork.Repository<TEntity>().AddAsync(entity);
            var result = await _unitOfWork.SaveAsync();
            if (result == false)
                return new BadRequestObjectResult("Failed to create entity");
            return _mapper.Map<TDTO>(entity);
        }

        public virtual async Task<ActionResult<TDTO>> Update(int id, TDTO dto)
        {
            var existingEntity = await _unitOfWork.Repository<TEntity>().GetByIdAsync(id);
            if (existingEntity == null)
                return new BadRequestObjectResult($"Entity not found");
            
            _mapper.Map(dto, existingEntity);
            
            _unitOfWork.Repository<TEntity>().Update(existingEntity);
            var result = await _unitOfWork.SaveAsync();
            if (result == false)
                return new BadRequestObjectResult("Failed to update entity");
            
            return _mapper.Map<TDTO>(existingEntity);
        }

        public virtual async Task<ActionResult<bool>> Delete(int id)
        {
            var entity = await _unitOfWork.Repository<TEntity>().GetByIdAsync(id);
            if (entity == null)
                return false;
            
            _unitOfWork.Repository<TEntity>().Delete(entity);
            
            return await _unitOfWork.SaveAsync();
        }

        public virtual async Task<ActionResult<TDTO>> GetByIdAsync(int id)
        {
            var entity = await _unitOfWork.Repository<TEntity>().GetByIdAsync(id);
            if (entity == null)
                return new BadRequestObjectResult($"Entity with ID {id} not found");
            
            return _mapper.Map<TDTO>(entity);
        }

        public virtual async Task<ActionResult<ICollection<TDTO>>> GetAllAsync()
        {
            var entities = await _unitOfWork.Repository<TEntity>().GetAllAsync();
            var mappedEntities = _mapper.Map<ICollection<TDTO>>(entities);
            return new OkObjectResult(mappedEntities);
        }
        
        public virtual async Task<ActionResult<ICollection<TDTO>>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            var entities = await _unitOfWork.Repository<TEntity>().FindAsync(predicate);   
            return new OkObjectResult(_mapper.Map<ICollection<TDTO>>(entities));
        }
    }
}