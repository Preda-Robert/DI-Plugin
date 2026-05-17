using System;
using API.DTO;
using API.Entities;
using API.Interfaces;
using API.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class UserRepository : BaseRepository<ApplicationUser>, IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    public UserRepository(DataContext context, UserManager<ApplicationUser> userManager) : base(context)
    {
        _userManager = userManager;
    }

    public async Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationUser user)
    {
        return await _userManager.UpdateAsync(user);
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationUser user)
    {
        return await _userManager.DeleteAsync(user);
    }

    public async Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role)
    {
        return await _userManager.AddToRoleAsync(user, role);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user)
    {
        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<bool> IsUsernameTakenAsync(string username)
    {
        return await _userManager.Users.AnyAsync(x => x.UserName == username);
    }

    public async Task<bool> IsEmailTakenAsync(string email)
    {
        return await _userManager.Users.AnyAsync(x => x.Email == email);
    }
    public async Task<ApplicationUser?> GetUserByIdAsync(int id)
    {
        return await _userManager.Users
            .Include(x => x.Photo)
            .Include(x => x.DietaryPreferences)
            .Include(x => x.ShoppingLists)
            .Include(x => x.MealPlans)
            .SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task<ApplicationUser?> GetUserByUsernameAsync(string username)
    {
        return await _userManager.Users.SingleOrDefaultAsync(x => x.UserName == username);
    }

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        return await _userManager.Users.SingleOrDefaultAsync(x => x.Email == email);
    }

    public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
    {
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> IsEmailConfirmedAsync(ApplicationUser user)
    {
        return await _userManager.IsEmailConfirmedAsync(user);
    }

    public async Task<IdentityResult> ConfirmEmailAsync(ApplicationUser user, string token)
    {
        return await _userManager.ConfirmEmailAsync(user, token);
    }

    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
    {
        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<string> GenerateEmailVerificationCodeAsync(ApplicationUser user)
    {
        
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();
        bool codeExists = true;
        while(codeExists)
        {
            code = random.Next(100000, 999999).ToString();
            codeExists = await _userManager.Users.AnyAsync(x => x.EmailVerificationCode == code);
        }

        user.EmailVerificationCode = code;
        user.EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(15);

        await _userManager.UpdateAsync(user);

        return code;
    }

    public async Task<bool> VerifyEmailVerificationCodeAsync(ApplicationUser user, string code)
    {

        if (user.EmailVerificationCode == code &&
            user.EmailVerificationCodeExpiry.HasValue &&
            user.EmailVerificationCodeExpiry.Value > DateTime.UtcNow)
        {
            user.EmailVerificationCode = null;
            user.EmailVerificationCodeExpiry = null;

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            return true;
        }

        return false;
    }
    public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
    {
        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<string> GeneratePasswordResetCodeAsync(ApplicationUser user)
    {
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();

        user.PasswordResetCode = code;
        user.PasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(15);

        await _userManager.UpdateAsync(user);

        return code;
    }

    public async Task<bool> VerifyPasswordResetCodeAsync(ApplicationUser user, string code)
    {
        if (user.PasswordResetCode == code &&
            user.PasswordResetCodeExpiry.HasValue &&
            user.PasswordResetCodeExpiry.Value > DateTime.UtcNow)
        {
            user.PasswordResetCode = null;
            user.PasswordResetCodeExpiry = null;

            await _userManager.UpdateAsync(user);

            return true;
        }

        return false;
    }

    public async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
    {
        return await _userManager.ResetPasswordAsync(user, token, newPassword);
    }

    async Task<bool> IUserRepository.ConfirmEmailAsync(ApplicationUser user, string token)
    {
        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            return true;
        }
        return false;
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(int id, bool isCurrentUser = false)
    {
        var query = _context.Users
            .Include(x => x.Photo)
            .Include(x => x.DietaryPreferences)
            .Include(x => x.ShoppingLists)
            .Include(x => x.MealPlans)
            .Include(x => x.Recipes)
            .Include(x => x.FavoriteRecipes)
            .AsQueryable();
        if (isCurrentUser)
        {
                // query filters??
        }
        return await query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<ApplicationUser?> GetUserByUsernameAsync(string username, bool isCurrentUser = false)
    {
        var query = _context.Users
            .Include(x => x.Photo)
            .Include(x => x.DietaryPreferences)
            .Include(x => x.ShoppingLists)
            .Include(x => x.MealPlans)
            .Include(x => x.Recipes)
            .Include(x => x.FavoriteRecipes)
            .AsQueryable();
        if (isCurrentUser)
        {
                // query filters??
        }
        return await query.FirstOrDefaultAsync(x => x.UserName == username);
    }
}
