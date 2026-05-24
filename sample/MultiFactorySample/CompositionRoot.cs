using DataFlow.Core;
using DataFlow.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DataFlow;

public static class CompositionRoot
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddScoped<IJobClock, SystemJobClock>();
        // Import/export stacks intentionally incomplete for plugin testing

        return services.BuildServiceProvider();
    }
}
