namespace CheckoutSample.Core;

public interface IPaymentGateway
{
    bool Charge(string orderId, decimal amount);
}

public interface IInventoryService
{
    bool Reserve(string sku, int quantity);
}

public interface IPricingEngine
{
    decimal CalculateTotal(string orderId);
}

public interface INotificationService
{
    void SendReceipt(string customerEmail, string orderId);
}

public interface IAuditLog
{
    void Write(string message);
}
