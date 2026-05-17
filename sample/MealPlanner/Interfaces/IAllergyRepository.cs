using System;
using API.Entities;
using API.Helpers;

namespace API.Interfaces;

public interface IAllergyRepository : IBaseRepository<Allergy>
{
    Task<Allergy?> GetAllergyByNameAsync(string name);
    Task<bool> AllergyExistsAsync(string name);
    Task<bool> AllergyExistsAsync(int id);
    Task<ICollection<Allergy>> GetAllAllergiesAsync();
    Task<ICollection<Allergy>> GetAllAllergiesByUserIdAsync(string userId);
    IQueryable<Allergy> GetAllergiesAsync(AllergyParams allergyParams);
}
