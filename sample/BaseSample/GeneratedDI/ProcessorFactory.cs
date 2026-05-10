using DemoApp;
using DemoApp.Core;
using DemoApp.Services;

namespace DemoApp.Services.Generated
{
    public sealed class ProcessorFactory
    {
        public Processor Create(IMessageService messageService, ILogger logger) => new Processor(messageService, logger);
    }
}
