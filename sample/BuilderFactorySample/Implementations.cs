using ShopApp.Core;

namespace ShopApp.Infrastructure
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message) { }
    }

    public class SystemClock : IClock
    {
        public string UtcNowIso() => "2026-05-10T00:00:00Z";
    }

    public class BasicTemplateEngine : ITemplateEngine
    {
        public string RenderOrder(int orderId) => $"Order #{orderId}";
    }

    public class SmtpEmailSender : IEmailSender
    {
        public void Send(string to, string body) { }
    }
}

