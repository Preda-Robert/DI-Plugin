namespace ShopApp.Core
{
    public interface ILogger
    {
        void Log(string message);
    }

    public interface IClock
    {
        string UtcNowIso();
    }

    public interface ITemplateEngine
    {
        string RenderOrder(int orderId);
    }

    public interface IEmailSender
    {
        void Send(string to, string body);
    }
}

