using DemoApp.Services;

namespace DemoApp.Core
{
    public class Processor : IProcessor
    {
        private readonly IMessageService _messageService;
        private readonly ILogger _logger;

        public Processor(IMessageService messageService, ILogger logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        public void Run()
        {
            var msg = _messageService.GetMessage();
            _logger.Log(msg);
        }
    }
}