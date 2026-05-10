using System;
using Microsoft.Extensions.DependencyInjection;
using ShopApp;
using ShopApp.Core;
using ShopApp.Infrastructure;
using ShopApp.Runner;
using ShopApp.Services;

namespace ShopApp.Infrastructure.Generated
{
    public static class AppBuilder
    {
        public static IServiceProvider Build()
        {
            var services = new ServiceCollection();
            services.AddScoped<IClock, SystemClock>();
            services.AddScoped<IEmailSender, SmtpEmailSender>();
            services.AddScoped<ILogger, ConsoleLogger>();
            services.AddScoped<ITemplateEngine, BasicTemplateEngine>();
            return services.BuildServiceProvider();
        }
    }
}
