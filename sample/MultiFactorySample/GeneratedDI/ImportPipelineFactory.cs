using System;
using Microsoft.Extensions.DependencyInjection;
using DataFlow.Core;
using DataFlow.Services;

namespace DataFlow.DI.Generated
{
    public sealed class ImportPipelineFactory
    {
        private readonly IServiceProvider _provider;

        public ImportPipelineFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ImportPipeline Create() => new ImportPipeline(_provider.GetRequiredService<IImportSource>(), _provider.GetRequiredService<IImportValidator>(), _provider.GetRequiredService<IImportStorage>());
    }
}
