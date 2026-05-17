using System;

namespace API.Helpers;

public class GoogleSettings
{
    public const string GoogleSettingsKey = "GoogleSettings";
    public required string Gmail { get; set; } = string.Empty;
    public required string Password { get; set; } = string.Empty;
    public required string SMTPServer { get; set; } = string.Empty;
    public required int SMTPPort { get; set; } = 0;
}
