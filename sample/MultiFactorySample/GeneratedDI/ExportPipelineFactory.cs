using System;
using Microsoft.Extensions.DependencyInjection;
using DataFlow.Core;
using DataFlow.Services;

namespace DataFlow.DI.Generated
{
    public sealed class ExportPipelineFactory
    {
        private readonly IServiceProvider _provider;

        public ExportPipelineFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ExportPipeline Create() => new ExportPipeline(_provider.GetRequiredService<IExportSource>(), _provider.GetRequiredService<IExportFormatter>(), _provider.GetRequiredService<IExportDelivery>());
    }
}
