using ShopApp.Core;
using ShopApp.Infrastructure;

namespace ShopApp.Services
{
    // Good DI shape: multiple interface dependencies -> should trigger factory suggestion.
    public class OrderNotificationService
    {
        private readonly ILogger _logger;
        private readonly ITemplateEngine _templateEngine;
        private readonly IEmailSender _emailSender;
        private readonly IClock _clock;

        public OrderNotificationService(
            ILogger logger,
            ITemplateEngine templateEngine,
            IEmailSender emailSender,
            IClock clock)
        {
            _logger = logger;
            _templateEngine = templateEngine;
            _emailSender = emailSender;
            _clock = clock;
        }

        public void Notify(int orderId, string email)
        {
            var body = _templateEngine.RenderOrder(orderId);
            _emailSender.Send(email, body);
            _logger.Log(_clock.UtcNowIso());
        }
    }

    // DI smell: concrete dependency to test analyzer warnings.
    public class ConcreteMailerService
    {
        public ConcreteMailerService(SmtpEmailSender smtp) { }
    }
}

