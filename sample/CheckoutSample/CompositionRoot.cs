using CheckoutSample.Core;
using CheckoutSample.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CheckoutSample;

public static class CompositionRoot
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        // Partial wiring — plugin should suggest the rest + CheckoutCoordinatorFactory
        services.AddScoped<IInventoryService, WarehouseInventory>();
        services.AddScoped<IPricingEngine, StandardPricing>();
        services.AddScoped<IAuditLog, ConsoleAuditLog>();

        return services.BuildServiceProvider();
    }
}
