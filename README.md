# Dependency Injection Plugin

**VS Code extension** for **C#** that helps with dependency injection: analyze constructors and their dependencies, detect issues, and (later) suggest registrations and builders/factories.

---

## Features (current)

- **C# parsing** with tree-sitter: finds classes and constructors, extracts parameter types (dependencies).
- **Command:** **"DI: Analyze current file"** — lists all constructors, their dependencies, and **DI issues** in the Output panel (channel: "Dependency Injection").
- **Diagnostics:** opening or editing a C# file shows:
  - Informational hints on constructors with their dependency list.
  - **Warnings** for DI issues: concrete types in constructors (prefer interfaces) and circular dependencies within the file.

## How to run

```bash
npm install
```

Press **F5** in VS Code to launch the Extension Development Host. Open a `.cs` file and:

- Run **Ctrl+Shift+P** → **"DI: Analyze current file"** to see the full report in the Output panel.
- Constructors are underlined with an informational message listing their dependencies.

## Roadmap

| Phase | Goal |
|-------|------|
| **1** ✅ | Parse C# and find constructors + dependencies (done). |
| **2** ✅ | Detect DI issues: circular dependencies, concrete types instead of interfaces (done). Missing registrations need project-wide analysis (Phase 3). |
| **3** | Suggest or generate registration code (e.g. for `Microsoft.Extensions.DependencyInjection`). |
| **4** | Quick fixes and refactors (e.g. "Add to DI container", "Extract interface and register"). |

## Tech

- **Parsing:** [tree-sitter](https://tree-sitter.github.io/) + [tree-sitter-c-sharp](https://github.com/tree-sitter/tree-sitter-c-sharp).
- **IDE:** VS Code Extension API (diagnostics, commands, Output channel).
