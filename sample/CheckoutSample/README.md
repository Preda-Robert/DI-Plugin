# CheckoutSample

Practical **e-commerce checkout** mini-app for testing factory generation.

## What to test

| Command | Expected |
|---------|----------|
| **DI: Suggest factories for workspace** | `CheckoutCoordinatorFactory` (4 interface dependencies) |
| **DI: Suggest registrations for workspace** | Missing `IPaymentGateway`, `INotificationService`, etc. |
| **DI: Generate builders/factories files** | `CheckoutCoordinatorFactory.cs` + `AppBuilder.cs` (factory only, not `CheckoutCoordinator` + factory) |
| **DI: Analyze workspace** | No false positives for `IServiceProvider` on factories |

## Domain

- **CheckoutCoordinator** — orchestrates payment, inventory, pricing, and customer notification (typical “too many ctor deps” service).
- **PaymentCaptureService** — only 2 dependencies → should **not** get a generated factory.
- **CompositionRoot** — partial wiring (inventory + pricing only); rest left for the plugin.

Open this folder as the VS Code workspace root, then run commands from the palette.
