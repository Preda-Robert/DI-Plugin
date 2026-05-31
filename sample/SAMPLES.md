# DI Plugin — sample projects

Open a sample folder as the **VS Code workspace root** (File → Open Folder), then use the Command Palette.

## Registration & analysis

| Sample | Purpose |
|--------|---------|
| [BaseSample](BaseSample/) | No DI wired — registration suggest/generate from scratch |
| [BuilderFactorySample](BuilderFactorySample/) | Original factory demo + concrete-type smell + merge into `CompositionRoot` |
| [MealPlanner](MealPlanner/) | Full ASP.NET API — `AddApplicationServices`, AutoMapper, Identity (false-positive checks) |

## Factory / builder commands

| Sample | Factory candidates | Notes |
|--------|-------------------|--------|
| [BuilderFactorySample](BuilderFactorySample/) | `OrderNotificationService` (4 deps) | Also `ConcreteMailerService` DI smell |
| [CheckoutSample](CheckoutSample/) | `CheckoutCoordinator` (4 deps) | `PaymentCaptureService` (2 deps) should **not** get a factory |
| [MultiFactorySample](MultiFactorySample/) | `ImportPipeline`, `ExportPipeline` (3 deps each) | Tests **multiple** generated factories in one workspace |

### Suggested workflow (factory)

1. **DI: Suggest factories for workspace** — preview `*Factory.cs` snippets only.
2. **DI: Generate builders/factories files (workspace)** — write `GeneratedDI/`.
3. **DI: Merge GeneratedDI into CompositionRoot** — copy `services.Add*` into `CompositionRoot.cs`.

Factory rule used by the plugin: **≥ 3 constructor parameters**, each type is a known **interface→implementation** pair in the workspace.
