using System;
using Microsoft.Extensions.DependencyInjection;
using EventPlanner.Repository;
using EventPlanner.Repository.Interfaces;

namespace EventPlanner.DI.Generated
{
    public static class AppBuilder
    {
        // NOTE: Generated AppBuilder is intended to replace/supersede manual CompositionRoot wiring.
        public static IServiceProvider Build()
        {
            var services = new ServiceCollection();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IGuestRepository, GuestRepository>();
            services.AddScoped<IHostRepository, HostRepository>();
            services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
            return services.BuildServiceProvider();
        }
    }
}
