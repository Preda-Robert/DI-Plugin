# Dependency Injection Plugin

**VS Code extension** for **C#** that helps with dependency injection: analyze constructors and their dependencies, detect issues, suggest or generate `Microsoft.Extensions.DependencyInjection` registrations, and scaffold builders/factories.

---

## Features (current)

- **C# parsing** with tree-sitter: finds classes and constructors, extracts parameter types (dependencies).
- **Commands (file):** **"DI: Analyze current file"**, **"DI: Suggest registrations for current file"** — Output channel **Dependency Injection** lists constructors, dependencies, and DI issues.
- **Commands (workspace):** **"DI: Analyze workspace"**, **"DI: Suggest registrations for workspace"** — cross-file missing registration hints and registration lines inferred from `public class Impl : IService` patterns plus `AddSingleton|AddScoped|AddTransient<...>` scanning.
- **Commands (builders/factories):** **"DI: Suggest builders/factories for current file"** (with workspace fallback), **"DI: Suggest builders/factories for workspace"**, **"DI: Generate builders/factories files (workspace)"** — writes `GeneratedDI/` (e.g. `AppBuilder.cs`, `*Factory.cs`) using `Microsoft.Extensions.DependencyInjection`.
- **Diagnostics:** opening or editing a C# file shows:
  - Informational hints on constructors with their dependency list.
  - **Warnings** for DI issues: concrete types in constructors (prefer interfaces), circular dependencies within the file, and missing `Add*(<T,...>)` registrations **in the same file** (with Quick Fix where applicable).
- **Phase 4 — quick fixes & composition root:**
  - **Quick Fix:** insert registration in the current file (`ConfigureServices`) or **into the workspace composition root** (`CompositionRoot`, `Add*Services`, or `ConfigureServices`).
  - **Quick Fix:** **Extract `I…` interface and register** for concrete constructor parameters (creates interface file, updates implementation and parameter type, inserts registration).
  - **Command:** **"DI: Insert missing registrations (workspace)"** — writes suggested `services.Add*` lines into the detected composition root.
  - **Command:** **"DI: Merge GeneratedDI into CompositionRoot"** — copies registrations from `GeneratedDI/AppBuilder.cs` into your existing `CompositionRoot` / `AddApplicationServices` (skips duplicates).

## How to run

```bash
npm install
```

Press **F5** in VS Code to launch the Extension Development Host. Open a `.cs` file and:

- Run **Ctrl+Shift+P** → **"DI: Analyze current file"** to see the full report in the Output panel.
- Constructors are underlined with an informational message listing their dependencies.
- For registrations and generated DI wiring, use **"DI: Suggest registrations for current file"** / **"DI: Suggest registrations for workspace"** and **"DI: Generate builders/factories files (workspace)"** (see Features).

For workspace-level testing, open one of:
- `sample/BaseSample`
- `sample/BuilderFactorySample`
- `sample/MealPlanner` (ASP.NET Core API with `AddApplicationServices`)

Then run workspace commands from Command Palette. For **Phase 4**, try **BuilderFactorySample**: run **"DI: Merge GeneratedDI into CompositionRoot"** or **"DI: Insert missing registrations (workspace)"**, and use the lightbulb on `ConcreteMailerService`’s `SmtpEmailSender` parameter for **Extract interface and register**.

## Roadmap

| Phase | Goal |
|-------|------|
| **1** ✅ | Parse C# and find constructors + dependencies. |
| **2** ✅ | Detect DI issues: circular dependencies, concrete types instead of interfaces, same-file missing registrations; workspace-wide missing registration hints. |
| **3** ✅ | Suggest or generate registration code for `Microsoft.Extensions.DependencyInjection`: file + workspace suggestion commands; generated `AppBuilder` wiring inferred interface→implementation mappings and service registrations. |
| **4** ✅ | Quick fixes and composition-root workflows: workspace registration insert, extract interface and register, merge `GeneratedDI/AppBuilder` into `CompositionRoot` / `Add*Services`. |

## Tech

- **Parsing:** [tree-sitter](https://tree-sitter.github.io/) + [tree-sitter-c-sharp](https://github.com/tree-sitter/tree-sitter-c-sharp).
- **IDE:** VS Code Extension API (diagnostics, commands, Output channel).
