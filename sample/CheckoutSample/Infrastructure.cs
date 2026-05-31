namespace CheckoutSample.Infrastructure;

using CheckoutSample.Core;

public class StripePaymentGateway : IPaymentGateway
{
    public bool Charge(string orderId, decimal amount) => amount > 0;
}

public class WarehouseInventory : IInventoryService
{
    public bool Reserve(string sku, int quantity) => quantity > 0;
}

public class StandardPricing : IPricingEngine
{
    public decimal CalculateTotal(string orderId) => 42.50m;
}

public class EmailNotificationService : INotificationService
{
    public void SendReceipt(string customerEmail, string orderId) { }
}

public class ConsoleAuditLog : IAuditLog
{
    public void Write(string message) => Console.WriteLine(message);
}
