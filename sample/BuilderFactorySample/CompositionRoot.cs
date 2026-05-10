using Microsoft.Extensions.DependencyInjection;
using ShopApp.Core;
using ShopApp.Infrastructure;
using ShopApp.Services;

namespace ShopApp
{
    public static class CompositionRoot
    {
        // Intentionally incomplete registrations for plugin testing.
        public static IServiceProvider Build()
        {
            var services = new ServiceCollection();

            services.AddScoped<ILogger, ConsoleLogger>();
            services.AddScoped<IClock, SystemClock>();
            services.AddScoped<ITemplateEngine, BasicTemplateEngine>();
            // Missing on purpose: IEmailSender and OrderNotificationService

            return services.BuildServiceProvider();
        }
    }
}

