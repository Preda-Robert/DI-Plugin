using System;
using Microsoft.Extensions.DependencyInjection;
using ShopApp.Core;
using ShopApp.Services;

namespace ShopApp.DI.Generated
{
    public sealed class OrderNotificationServiceFactory
    {
        private readonly IServiceProvider _provider;

        public OrderNotificationServiceFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public OrderNotificationService Create() => new OrderNotificationService(_provider.GetRequiredService<ILogger>(), _provider.GetRequiredService<ITemplateEngine>(), _provider.GetRequiredService<IEmailSender>(), _provider.GetRequiredService<IClock>());
    }
}
