namespace CheckoutSample.Services;

using CheckoutSample.Core;

/// <summary>
/// Coordinates checkout — 4 interface deps → plugin should suggest/generate a factory.
/// </summary>
public class CheckoutCoordinator
{
    private readonly IPaymentGateway _payment;
    private readonly IInventoryService _inventory;
    private readonly IPricingEngine _pricing;
    private readonly INotificationService _notifications;

    public CheckoutCoordinator(
        IPaymentGateway payment,
        IInventoryService inventory,
        IPricingEngine pricing,
        INotificationService notifications)
    {
        _payment = payment;
        _inventory = inventory;
        _pricing = pricing;
        _notifications = notifications;
    }

    public bool RunCheckout(string orderId, string sku, int qty, string email)
    {
        if (!_inventory.Reserve(sku, qty)) return false;
        var total = _pricing.CalculateTotal(orderId);
        if (!_payment.Charge(orderId, total)) return false;
        _notifications.SendReceipt(email, orderId);
        return true;
    }
}

/// <summary>
/// Simple service — only 2 deps → should NOT get a factory (direct DI is fine).
/// </summary>
public class PaymentCaptureService
{
    private readonly IPaymentGateway _payment;
    private readonly IAuditLog _audit;

    public PaymentCaptureService(IPaymentGateway payment, IAuditLog audit)
    {
        _payment = payment;
        _audit = audit;
    }

    public void Capture(string orderId, decimal amount)
    {
        _payment.Charge(orderId, amount);
        _audit.Write($"Captured {orderId}");
    }
}
