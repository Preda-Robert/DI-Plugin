using System;
using Microsoft.Extensions.DependencyInjection;
using DataFlow.Core;
using DataFlow.Infrastructure;
using DataFlow.Services;

namespace DataFlow.DI.Generated
{
    public static class AppBuilder
    {
        // Factory-backed services are registered via *Factory only (not the concrete type). Merge lines into CompositionRoot or use as a checklist.
        public static IServiceProvider Build()
        {
            var services = new ServiceCollection();
            services.AddScoped<ExportPipelineFactory>();
            services.AddScoped<IExportDelivery, SftpExportDelivery>();
            services.AddScoped<IExportFormatter, JsonExportFormatter>();
            services.AddScoped<IExportSource, DatabaseExportSource>();
            services.AddScoped<IImportSource, CsvImportSource>();
            services.AddScoped<IImportStorage, SqlImportStorage>();
            services.AddScoped<IImportValidator, StrictImportValidator>();
            services.AddScoped<ImportPipelineFactory>();
            services.AddScoped<JobScheduler>();
            return services.BuildServiceProvider();
        }
    }
}
