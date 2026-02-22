// Sample C# file for testing the DI Plugin. Open this and run "DI: Analyze current file".
// - OrderService uses interfaces (good).
// - BadOrderService depends on concrete ProductRepository (DI issue: prefer interface).
// - ServiceA / ServiceB form a circular dependency (DI issue).

namespace SampleApp
{
    public interface ILogger { }
    public interface IEmailSender { }

    public class OrderService
    {
        private readonly ILogger _logger;
        private readonly IEmailSender _emailSender;

        public OrderService(ILogger logger, IEmailSender emailSender)
        {
            _logger = logger;
            _emailSender = emailSender;
        }
    }

    public class ProductRepository
    {
        public ProductRepository()
        {
        }
    }

    // DI issue: concrete type in constructor — prefer IProductRepository
    public class BadOrderService
    {
        public BadOrderService(ProductRepository repo)
        {
        }
    }

    // Circular dependency: ServiceA -> ServiceB -> ServiceA
    public class ServiceA
    {
        public ServiceA(ServiceB b) { }
    }

    public class ServiceB
    {
        public ServiceB(ServiceA a) { }
    }
}
