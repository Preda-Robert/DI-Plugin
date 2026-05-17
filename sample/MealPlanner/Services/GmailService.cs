using System;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using API.Interfaces;
using API.DTO;
using Microsoft.Extensions.Options;
using API.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace API.Services;

public class GmailService : IEmailService
{

    private readonly GoogleSettings _googleSettings;
    public GmailService(IOptions<GoogleSettings> googleSettings)
    {
        _googleSettings = googleSettings.Value;
    }

    public async Task<IActionResult> SendEmailAsync(SendEmailRequest sendEmailRequest)
    {   
        if (string.IsNullOrEmpty(sendEmailRequest.Recipient))
        {
            return new BadRequestObjectResult("Recipient email is null or empty");
        }

        try
        {
            MailMessage mailMessage = new MailMessage(_googleSettings.Gmail, sendEmailRequest.Recipient)
            {
                Subject = sendEmailRequest.Subject,
                Body = sendEmailRequest.Body,
                IsBodyHtml = true
            };

            using var smtpClient = new SmtpClient();
            smtpClient.Host = _googleSettings.SMTPServer;
            smtpClient.Port = _googleSettings.SMTPPort;
            smtpClient.Credentials = new NetworkCredential(_googleSettings.Gmail, _googleSettings.Password);
            smtpClient.EnableSsl = true; 

            await smtpClient.SendMailAsync(mailMessage);
            return new OkObjectResult("Email sent successfully");
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult($"Failed to send email: {ex.Message}");
        }
    }
}