# Dependency Injection Plugin

**VS Code extension** for **C#** that helps with dependency injection: analyze constructors and their dependencies, detect issues, suggest or generate `Microsoft.Extensions.DependencyInjection` registrations, and scaffold builders/factories.

---

## Features (current)

- **C# parsing** with tree-sitter: finds classes and constructors, extracts parameter types (dependencies).
- **Commands (file):** **"DI: Analyze current file"**, **"DI: Suggest registrations for current file"** — Output channel **Dependency Injection** lists constructors, dependencies, and DI issues.
- **Commands (workspace):** **"DI: Analyze workspace"**, **"DI: Suggest registrations for workspace"** — `services.Add*` lines for your composition root (what to register in DI).
- **Commands (factories):** **"DI: Suggest factories for current file"** / **workspace** — `*Factory.cs` classes for services with **3+ interface constructor dependencies** (how to *construct* a complex service via `IServiceProvider`). Does **not** repeat registration lines; use registration commands for `Add*`.
- **Command (generate):** **"DI: Generate builders/factories files (workspace)"** — writes `GeneratedDI/` (`AppBuilder.cs` + `*Factory.cs`) to disk; merge AppBuilder registrations with **"DI: Merge GeneratedDI into CompositionRoot"**.

| You want… | Command |
|-----------|---------|
| Lines to paste into `CompositionRoot` / `AddApplicationServices` | Suggest **registrations** (workspace) or **Insert missing registrations** |
| A factory class for a service with many interface deps | Suggest **factories** (workspace) |
| Files on disk (`GeneratedDI/`) | **Generate** builders/factories files |
- **Diagnostics:** opening or editing a C# file shows:
  - Informational hints on constructors with their dependency list.
  - **Warnings** for DI issues: concrete types in constructors (prefer interfaces), circular dependencies within the file, and missing `Add*(<T,...>)` registrations **in the same file** (with Quick Fix where applicable).
- **Phase 4 — quick fixes & composition root:**
  - **Quick Fix:** insert registration in the current file (`ConfigureServices`) or **into the workspace composition root** (`CompositionRoot`, `Add*Services`, or `ConfigureServices`).
  - **Quick Fix:** **Extract `I…` interface and register** for concrete constructor parameters (creates interface file, updates implementation and parameter type, inserts registration).
  - **Command:** **"DI: Insert missing registrations (workspace)"** — writes suggested `services.Add*` lines into the detected composition root.
  - **Command:** **"DI: Merge GeneratedDI into CompositionRoot"** — copies registrations from `GeneratedDI/AppBuilder.cs` into your existing `CompositionRoot` / `AddApplicationServices` (skips duplicates).

## How to run

Download the .vsix extension and run this command to install it for you Visual Studio Code:

```bash
code --install-extension di-plugin-0.0.1.vsix
```

OR

```bash
npm install
```

Press **F5** in VS Code to launch the Extension Development Host. Open a `.cs` file and:

- Run **Ctrl+Shift+P** → **"DI: Analyze current file"** to see the full report in the Output panel.
- Constructors are underlined with an informational message listing their dependencies.
- For registrations and generated DI wiring, use **"DI: Suggest registrations for current file"** / **"DI: Suggest registrations for workspace"** and **"DI: Generate builders/factories files (workspace)"** (see Features).

For workspace-level testing, open one sample folder as the workspace root (see [sample/SAMPLES.md](sample/SAMPLES.md)):

| Sample | Best for |
|--------|----------|
| `sample/BaseSample` | Registration suggest from empty project |
| `sample/BuilderFactorySample` | Single factory + merge / extract-interface quick fixes |
| `sample/CheckoutSample` | Checkout orchestrator (4 deps) + simple 2-dep service (no factory) |
| `sample/MultiFactorySample` | **Two** factories (import + export pipelines) in one workspace |
| `sample/MealPlanner` | Full ASP.NET API, AutoMapper, Identity |

**Factory commands:** open `CheckoutSample` or `MultiFactorySample` → **DI: Suggest factories for workspace** → **Generate builders/factories files** → **Merge GeneratedDI into CompositionRoot**.

## Tech

- **Parsing:** [tree-sitter](https://tree-sitter.github.io/) + [tree-sitter-c-sharp](https://github.com/tree-sitter/tree-sitter-c-sharp).
- **IDE:** VS Code Extension API (diagnostics, commands, Output channel).
