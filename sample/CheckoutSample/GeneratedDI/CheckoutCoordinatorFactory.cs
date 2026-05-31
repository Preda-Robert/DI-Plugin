using System;
using Microsoft.Extensions.DependencyInjection;
using CheckoutSample.Core;
using CheckoutSample.Services;

namespace CheckoutSample.DI.Generated
{
    public sealed class CheckoutCoordinatorFactory
    {
        private readonly IServiceProvider _provider;

        public CheckoutCoordinatorFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public CheckoutCoordinator Create() => new CheckoutCoordinator(_provider.GetRequiredService<IPaymentGateway>(), _provider.GetRequiredService<IInventoryService>(), _provider.GetRequiredService<IPricingEngine>(), _provider.GetRequiredService<INotificationService>());
    }
}
