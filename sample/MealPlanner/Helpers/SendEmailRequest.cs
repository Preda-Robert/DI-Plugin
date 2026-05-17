using System;

namespace API.Helpers;

public record SendEmailRequest
{
    public string Recipient { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public SendEmailRequest(string recipient, string subject, string body)
    {
        Recipient = recipient;
        Subject = subject;
        Body = body;
    }
}
