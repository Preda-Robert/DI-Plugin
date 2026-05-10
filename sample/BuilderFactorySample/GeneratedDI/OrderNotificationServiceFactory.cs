using ShopApp;
using ShopApp.Core;
using ShopApp.Infrastructure;
using ShopApp.Runner;
using ShopApp.Services;

namespace ShopApp.Infrastructure.Generated
{
    public sealed class OrderNotificationServiceFactory
    {
        public OrderNotificationService Create(ILogger logger, ITemplateEngine templateEngine, IEmailSender emailSender, IClock clock) => new OrderNotificationService(logger, templateEngine, emailSender, clock);
    }
}
