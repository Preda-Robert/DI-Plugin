using System;
using Microsoft.Extensions.DependencyInjection;
using DemoApp;
using DemoApp.Core;
using DemoApp.Services;

namespace DemoApp.Services.Generated
{
    public static class AppBuilder
    {
        public static IServiceProvider Build()
        {
            var services = new ServiceCollection();
            services.AddScoped<ILogger, ConsoleLogger>();
            services.AddScoped<IMessageService, HelloMessageService>();
            services.AddScoped<IProcessor, Processor>();
            return services.BuildServiceProvider();
        }
    }
}
