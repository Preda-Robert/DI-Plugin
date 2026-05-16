using System;
using Microsoft.Extensions.DependencyInjection;
using DemoApp.Core;
using DemoApp.Services;

namespace DemoApp.DI.Generated
{
    public static class AppBuilder
    {
        // NOTE: Generated AppBuilder is intended to replace/supersede manual CompositionRoot wiring.
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
