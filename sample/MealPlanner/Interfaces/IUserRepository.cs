using System;
using API.DTO;
using API.Entities;
using Microsoft.AspNetCore.Identity;

namespace API.Interfaces;

public interface IUserRepository : IBaseRepository<ApplicationUser>
{
    Task<IdentityResult> CreateAsync(ApplicationUser user, string password);
    Task<IdentityResult> UpdateAsync(ApplicationUser user); 
    Task<IdentityResult> DeleteAsync(ApplicationUser user);
    Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role);
    Task<ApplicationUser?> GetUserByIdAsync(int id);
    Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user);
    Task<bool> IsEmailConfirmedAsync(ApplicationUser user);
    Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
    Task<ApplicationUser?> GetUserByUsernameAsync(string username);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task<bool> IsUsernameTakenAsync(string username);
    Task<bool> IsEmailTakenAsync(string email);
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
    Task<string> GenerateEmailVerificationCodeAsync(ApplicationUser user);
    Task<bool> VerifyEmailVerificationCodeAsync(ApplicationUser user, string code);
    Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);
    Task<string> GeneratePasswordResetCodeAsync(ApplicationUser user);
    Task<bool> VerifyPasswordResetCodeAsync(ApplicationUser user, string code);
    Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword);
    Task<bool> ConfirmEmailAsync(ApplicationUser user, string token);
    Task<ApplicationUser?> GetUserByIdAsync(int id, bool isCurrentUser = false);
    Task<ApplicationUser?> GetUserByUsernameAsync(string username, bool isCurrentUser = false);
}
