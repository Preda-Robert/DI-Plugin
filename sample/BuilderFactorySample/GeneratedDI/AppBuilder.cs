using System;
using Microsoft.Extensions.DependencyInjection;
using ShopApp.Core;
using ShopApp.Infrastructure;
using ShopApp.Services;

namespace ShopApp.DI.Generated
{
    public static class AppBuilder
    {
        // NOTE: Generated AppBuilder is intended to replace/supersede manual CompositionRoot wiring.
        public static IServiceProvider Build()
        {
            var services = new ServiceCollection();
            services.AddScoped<ConcreteMailerService>();
            services.AddScoped<IEmailSender, SmtpEmailSender>();
            services.AddScoped<OrderNotificationService>();
            services.AddScoped<OrderNotificationServiceFactory>();
            return services.BuildServiceProvider();
        }
    }
}
