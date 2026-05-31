using System;
using Microsoft.Extensions.DependencyInjection;
using CheckoutSample.Core;
using CheckoutSample.Infrastructure;
using CheckoutSample.Services;

namespace CheckoutSample.DI.Generated
{
    public static class AppBuilder
    {
        // NOTE: Generated AppBuilder is intended to replace/supersede manual CompositionRoot wiring.
        public static IServiceProvider Build()
        {
            var services = new ServiceCollection();
            services.AddScoped<CheckoutCoordinator>();
            services.AddScoped<CheckoutCoordinatorFactory>();
            services.AddScoped<INotificationService, EmailNotificationService>();
            services.AddScoped<IPaymentGateway, StripePaymentGateway>();
            services.AddScoped<PaymentCaptureService>();
            return services.BuildServiceProvider();
        }
    }
}
