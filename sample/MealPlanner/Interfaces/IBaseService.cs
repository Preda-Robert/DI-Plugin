using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace API.Interfaces
{
    public interface IBaseService<TEntity, TDTO> 
        where TEntity : class 
        where TDTO : class
    {
        Task<ActionResult<TDTO>> Create(TDTO dto);
        Task<ActionResult<TDTO>> Update(int id, TDTO dto);
        Task<ActionResult<bool>> Delete(int id);
        Task<ActionResult<TDTO>> GetByIdAsync(int id);
        Task<ActionResult<ICollection<TDTO>>> GetAllAsync();
        Task<ActionResult<ICollection<TDTO>>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    }
}