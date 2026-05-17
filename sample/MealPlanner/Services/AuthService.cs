using System;
using API.DTO;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using PasswordGenerator;

namespace API.Services;

public class AuthService : BaseService<ApplicationUser, UserDTO>, IAuthService
{

    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;

    public AuthService(IUnitOfWork unitOfWork, IMapper mapper, ITokenService tokenService, IConfiguration config, IEmailService emailService) : base(unitOfWork, mapper)
    {
        _tokenService = tokenService;
        _config = config;
        _emailService = emailService;
    }

    public Task<bool> ValidateUserCredentialsAsync(ApplicationUser user, string password)
    {
        return _unitOfWork.UserRepository.CheckPasswordAsync(user, password);
    }
    public async Task<ActionResult<RegisterResponseDTO>> RegisterUser(RegisterDTO registerDTO)
    {
        var existingUser = await GetUserByUsernameOrEmailAsync(registerDTO.Username!, registerDTO.Email!);

        if (existingUser != null)
        {
            if (existingUser.UserName == registerDTO.Username && existingUser.Email != registerDTO.Email)
            {
                return new BadRequestObjectResult("Username is already taken");
            }
            if (existingUser.UserName != registerDTO.Username && existingUser.Email == registerDTO.Email)
            {
                return new BadRequestObjectResult("Email is already taken");
            }
            if (existingUser.UserName == registerDTO.Username && existingUser.Email == registerDTO.Email)
            {
                return new RegisterResponseDTO
                {
                    IsRegistered = true,
                    RequiresEmailConfirmation = existingUser.EmailConfirmed == false,
                    UserId = existingUser.Id
                };
            }

        }

        var user = _mapper.Map<ApplicationUser>(registerDTO);
        var createResult = await _unitOfWork.UserRepository.CreateAsync(user, registerDTO.Password!);
        await _unitOfWork.UserRepository.AddToRoleAsync(user, "Member");
        if (createResult.Succeeded == false)
        {
            return new BadRequestObjectResult("Problem creating user account");
        }

        var verificationCode = await _unitOfWork.UserRepository.GenerateEmailVerificationCodeAsync(user);

        SendEmailRequest sendEmailRequest = new SendEmailRequest(
            user.Email!,
            "Verify Your Email",
            $"<h1>Verify Your Email</h1><p>Your verification code is: <strong>{verificationCode}</strong></p><p>This code will expire in 15 minutes.</p>"
        );

        var emailResult = await _emailService.SendEmailAsync(sendEmailRequest);

        if (emailResult is BadRequestObjectResult badRequestResult)
        {
            return new BadRequestObjectResult("Problem sending email confirmation link: " + badRequestResult.Value);
        }

        return new RegisterResponseDTO
        {
            IsRegistered = false,
            RequiresEmailConfirmation = true,
            UserId = user.Id
        };
    }
    public async Task<ActionResult<UserDTO>> LoginUser(LoginDTO loginDTO)
    {

        if (string.IsNullOrEmpty(loginDTO.UsernameOrEmail) || string.IsNullOrEmpty(loginDTO.Password))
        {
            return new BadRequestObjectResult("Invalid credentials");
        }

        var userByUsername = await _unitOfWork.UserRepository.GetUserByUsernameAsync(loginDTO.UsernameOrEmail!);
        var userByEmail = await _unitOfWork.UserRepository.GetUserByEmailAsync(loginDTO.UsernameOrEmail!);

        if (userByUsername != null && userByEmail != null)
        {
            return new BadRequestObjectResult("Invalid credentials");
        }

        var user = userByUsername ?? userByEmail;
        if (user == null)
        {
            return new BadRequestObjectResult("Invalid credentials");
        }

        var result = await _unitOfWork.UserRepository.CheckPasswordAsync(user, loginDTO.Password);
        if (result == false)
        {
            return new UnauthorizedObjectResult("Invalid credentials");
        }

        var emailConfirmed = await _unitOfWork.UserRepository.IsEmailConfirmedAsync(user);
        if (emailConfirmed == false)
        {
            
            var verificationCode = await _unitOfWork.UserRepository.GenerateEmailVerificationCodeAsync(user);

            SendEmailRequest sendEmailRequest = new SendEmailRequest(user.Email!,
                "Email Verification Code",
                $"<h1>Verify Your Email</h1><p>Your verification code is: <strong>{verificationCode}</strong></p><p>This code will expire in 15 minutes.</p>"
            );

            var emailServiceResult = await _emailService.SendEmailAsync(sendEmailRequest);
            if (emailServiceResult is BadRequestObjectResult)
            {
                return new BadRequestObjectResult("Problem sending verification code");
            }
        }

        var token = await _tokenService.CreateToken(user);

        await _unitOfWork.UserRepository.UpdateAsync(user);

        var userDTO = _mapper.Map<UserDTO>(user);
        return userDTO;
    }
    public async Task<ActionResult<UserDTO>> GoogleAuth(GoogleDTO goolgeDTO)
    {
        var validationSettings = new GoogleJsonWebSignature.ValidationSettings()
        {
            Audience = [_config["Authentication:Google:ClientId"]]
        };
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(goolgeDTO.IdToken, validationSettings);
        }
        catch (Exception)
        {
            return new UnauthorizedObjectResult("Invalid credentials");
        }
        if (payload == null)
        {
            return new UnauthorizedObjectResult("Invalid credentials");
        }
        var user = await GetUserByUsernameOrEmailAsync(null!, payload.Email!);
        var name = payload.Name;
        name = name.Replace(" ", "");
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = name,
                DisplayName = name,
                Email = payload.Email
            };
            var pwd = new Password(includeLowercase: true, includeUppercase: false, includeNumeric: false, includeSpecial: false, passwordLength: 8);
            var password = pwd.Next();
            var result = await _unitOfWork.UserRepository.CreateAsync(user, password);
            if (result.Succeeded == false)
            {
                return new BadRequestObjectResult("Problem creating user account " + result.Errors.Select(x => x.Description).ToList());
            }
            await _unitOfWork.UserRepository.AddToRoleAsync(user, "Member");
        }

        user.EmailConfirmed = true;
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveAsync();

        var token = await _tokenService.CreateToken(user);

        return new UserDTO
        {
            UserName = user.UserName!,
            DisplayName = user.DisplayName,
            Token = token,
            PhotoUrl = user.Photo?.Url,
            EmailConfirmed = user.EmailConfirmed,
        };
    }
    public async Task<ActionResult> ConfirmEmailAsync(EmailConfirmationDTO emailConfirmationDTO)
    {
        var user = await _unitOfWork.UserRepository.GetUserByIdAsync(emailConfirmationDTO.Id);
        if (user == null)
        {
            return new BadRequestObjectResult("Invalid credentials");
        }
        var result = await _unitOfWork.UserRepository.ConfirmEmailAsync(user, emailConfirmationDTO.Token);
        if (result == false)
        {
            return new BadRequestObjectResult("Problem confirming email");
        }
        return new OkObjectResult("Email confirmed successfully");
    }
    public async Task<ActionResult<bool>> VerifyEmailAsync(EmailVerificationDTO verificationDTO)
    {
        var user = await _unitOfWork.UserRepository.GetUserByIdAsync(verificationDTO.Id);
        if (user == null)
        {
            return new BadRequestObjectResult("Invalid user ID");
        }

        var result = await _unitOfWork.UserRepository.VerifyEmailVerificationCodeAsync(user, verificationDTO.Code);
        if (!result)
        {
            return new BadRequestObjectResult("Invalid or expired verification code");
        }

        return new OkObjectResult(true);
    }

    public async Task<ActionResult> ResendVerificationCodeAsync(ResendVerificationCodeDTO resendDTO)
    {
        var user = await _unitOfWork.UserRepository.GetUserByIdAsync(resendDTO.UserId);
        if (user == null)
        {
            return new BadRequestObjectResult("Invalid user ID");
        }


        var verificationCode = await _unitOfWork.UserRepository.GenerateEmailVerificationCodeAsync(user);


        SendEmailRequest sendEmailRequest = new SendEmailRequest(
            user.Email!,
            "Verify Your Email",
            $"<h1>Verify Your Email</h1><p>Your new verification code is: <strong>{verificationCode}</strong></p><p>This code will expire in 15 minutes.</p>"
        );

        var emailResult = await _emailService.SendEmailAsync(sendEmailRequest);
        if (emailResult is BadRequestObjectResult badRequestResult)
        {
            return new BadRequestObjectResult("Problem sending verification code: " + badRequestResult.Value);
        }

        return new OkResult();
    }

    private async Task<ApplicationUser?> GetUserByUsernameOrEmailAsync(string? username, string? email)
    {
        ApplicationUser? user;
        if (!string.IsNullOrEmpty(username))
        {
            user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            if (user != null)
            {
                return user;
            }
        }
        if (!string.IsNullOrEmpty(email))
        {
            user = await _unitOfWork.UserRepository.GetUserByEmailAsync(email);
            return user;
        }
        return null;
    }
    public async Task<ActionResult> ForgotPasswordAsync(ForgotPasswordDTO forgotPasswordDTO)
    {

        var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(forgotPasswordDTO.Email);
        if (user == null)
        {
            return new OkObjectResult(new { message = "If your email is registered, you will receive a password reset code" });
        }

        var resetCode = await _unitOfWork.UserRepository.GeneratePasswordResetCodeAsync(user);

        SendEmailRequest sendEmailRequest = new SendEmailRequest(
            user.Email!,
            "Reset Your Password",
            $"<h1>Reset Your Password</h1><p>Your password reset code is: <strong>{resetCode}</strong></p><p>This code will expire in 15 minutes.</p>"
        );

        var emailResult = await _emailService.SendEmailAsync(sendEmailRequest);
        if (emailResult is BadRequestObjectResult badRequestResult)
        {

            Console.WriteLine($"Error sending password reset email: {badRequestResult.Value}");
        }


        return new OkObjectResult(new { message = "If your email is registered, you will receive a password reset code" });
    }

    public async Task<ActionResult> ResetPasswordAsync(ResetPasswordDTO resetPasswordDTO)
    {

        var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(resetPasswordDTO.Email);
        if (user == null)
        {
            return new BadRequestObjectResult("Invalid reset request");
        }

        var isCodeValid = await _unitOfWork.UserRepository.VerifyPasswordResetCodeAsync(user, resetPasswordDTO.Code);
        if (!isCodeValid)
        {
            return new BadRequestObjectResult("Invalid or expired reset code");
        }

        var token = await _unitOfWork.UserRepository.GeneratePasswordResetTokenAsync(user);
        var result = await _unitOfWork.UserRepository.ResetPasswordAsync(user, token, resetPasswordDTO.NewPassword);

        if (!result.Succeeded)
        {
            return new BadRequestObjectResult("Failed to reset password: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return new OkObjectResult(new { message = "Password has been reset successfully" });
    }
}
