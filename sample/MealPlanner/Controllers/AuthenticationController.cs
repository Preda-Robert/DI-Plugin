using Microsoft.AspNetCore.Mvc;
using API.DTOs;
using API.DTO;
using API.Services;
using API.Interfaces;
using Microsoft.AspNetCore.Authentication.Google;
namespace API.Controllers;
[Route("api/authentication")]
[ApiController]
public class AuthenticationController : BaseAPIController
{

    private readonly IUnitOfServices _unitOfServices;
    public AuthenticationController(IUnitOfServices unitOfServices)
    {
        _unitOfServices = unitOfServices;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponseDTO>> Register(RegisterDTO registerDTO)
    {
        var registerRespone = await _unitOfServices.AuthService.RegisterUser(registerDTO);
        return registerRespone;

    }

    [HttpGet("confirm-email")]
    public async Task<ActionResult<bool>> ConfirmEmail(EmailConfirmationDTO emailConfirmationDTO)
    {

        var confirmEmailResponse = await _unitOfServices.AuthService.ConfirmEmailAsync(emailConfirmationDTO);
        return confirmEmailResponse;

    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<bool>> VerifyEmail(EmailVerificationDTO verificationDTO)
    {
        var result = await _unitOfServices.AuthService.VerifyEmailAsync(verificationDTO);
        return result;
    }

    [HttpPost("resend-verification-code")]
    public async Task<ActionResult> ResendVerificationCode([FromBody] ResendVerificationCodeDTO request)
    {
        var result = await _unitOfServices.AuthService.ResendVerificationCodeAsync(request);
        return result;
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
    {
        var loginResponse = await _unitOfServices.AuthService.LoginUser(loginDTO);
        return loginResponse;
    }

    [HttpPost("google-auth")]
    public async Task<ActionResult<UserDTO>> GoogleAuth([FromBody] GoogleDTO googleDTO)
    {
        var googleAuthResponse = await _unitOfServices.AuthService.GoogleAuth(googleDTO);
        return googleAuthResponse;
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword(ForgotPasswordDTO forgotPasswordDTO)
    {
        return await _unitOfServices.AuthService.ForgotPasswordAsync(forgotPasswordDTO);
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword(ResetPasswordDTO resetPasswordDTO)
    {
        return await _unitOfServices.AuthService.ResetPasswordAsync(resetPasswordDTO);
    }
}