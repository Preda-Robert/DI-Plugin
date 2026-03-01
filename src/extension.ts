import * as vscode from "vscode";
import { analyzeCSharp, ConstructorInfo } from "./csharp/analyzer";

const DIAGNOSTIC_COLLECTION = "di-plugin";
const outputChannel = vscode.window.createOutputChannel("Dependency Injection");

function createRegistrationEdit(doc: vscode.TextDocument, typeName: string): vscode.WorkspaceEdit | undefined {
  const text = doc.getText();
  const signature = "ConfigureServices(IServiceCollection services)";
  const sigIndex = text.indexOf(signature);
  if (sigIndex === -1) {
    return undefined;
  }
  const braceIndex = text.indexOf("{", sigIndex + signature.length);
  if (braceIndex === -1) {
    return undefined;
  }

  const insertPos = doc.positionAt(braceIndex + 1);
  const edit = new vscode.WorkspaceEdit();
  edit.insert(doc.uri, insertPos, `\n            services.AddTransient<${typeName}>();`);
  return edit;
}

function buildRegistrationSuggestions(constructors: ConstructorInfo[], source: string): string[] {
  const suggestions = new Set<string>();

  // Discover classes and their base/interface list using simple regex.
  const classRegex = /public\s+class\s+(\w+)(\s*:\s*([^{\r\n]+))?/g;
  const allClasses = new Set<string>();
  const interfaceToImpl = new Map<string, Set<string>>();

  let match: RegExpExecArray | null;
  while ((match = classRegex.exec(source)) !== null) {
    const implName = match[1];
    allClasses.add(implName);
    const bases = match[3];
    if (!bases) continue;

    for (const rawBase of bases.split(",")) {
      const baseName = rawBase.split("<")[0].trim(); // strip generics
      // Heuristic: interface names like IFoo
      if (/^I[A-Z]/.test(baseName)) {
        if (!interfaceToImpl.has(baseName)) {
          interfaceToImpl.set(baseName, new Set());
        }
        interfaceToImpl.get(baseName)!.add(implName);
      }
    }
  }

  // 1) Suggest interface-to-implementation registrations where we see "class Impl : IFace"
  for (const [iface, impls] of interfaceToImpl.entries()) {
    for (const impl of impls) {
      suggestions.add(`services.AddScoped<${iface}, ${impl}>();`);
    }
  }

  // Track implementations already covered by interface suggestions
  const implUsedForInterface = new Set<string>();
  for (const impls of interfaceToImpl.values()) {
    for (const impl of impls) {
      implUsedForInterface.add(impl);
    }
  }

  // 2) For remaining classes with constructors, suggest simple AddTransient<Class>()
  for (const c of constructors) {
    const type = c.className;
    if (!type || implUsedForInterface.has(type)) continue;
    suggestions.add(`services.AddTransient<${type}>();`);
  }

  return Array.from(suggestions).sort();
}

class DiCodeActionProvider implements vscode.CodeActionProvider {
  provideCodeActions(
    document: vscode.TextDocument,
    _range: vscode.Range,
    context: vscode.CodeActionContext
  ): vscode.ProviderResult<vscode.CodeAction[]> {
    const actions: vscode.CodeAction[] = [];

    for (const diag of context.diagnostics) {
      if (diag.source !== DIAGNOSTIC_COLLECTION) continue;

      // Quick fix for missing registration diagnostics
      if (diag.message.startsWith("DI: No Add*(")) {
        const match = diag.message.match(/<([^,>]+),/);
        const paramType = match ? match[1].trim() : undefined;
        if (!paramType) continue;

        const edit = createRegistrationEdit(document, paramType);
        if (!edit) continue;

        const action = new vscode.CodeAction(
          `Add DI registration for ${paramType}`,
          vscode.CodeActionKind.QuickFix
        );
        action.diagnostics = [diag];
        action.edit = edit;
        actions.push(action);
      }
    }

    return actions;
  }
}

function analyzeCSharpDocument(
  doc: vscode.TextDocument,
  collection: vscode.DiagnosticCollection
): void {
  if (doc.languageId !== "csharp") return;

  const uri = doc.uri;
  const diagnostics: vscode.Diagnostic[] = [];

  const source = doc.getText();
  const { constructors, errors, concreteTypeIssues, circularDependencyIssues, missingRegistrationIssues } =
    analyzeCSharp(source);

  for (const c of constructors) {
    const range = new vscode.Range(
      doc.positionAt(c.startIndex),
      doc.positionAt(c.endIndex)
    );
    const depList = c.parameters.map((p) => `${p.type} {${p.name}}`).join(", ");
    const message =
      c.parameters.length === 0
        ? `Constructor has no dependencies.`
        : `Constructor dependencies: ${depList}`;
    diagnostics.push({
      range,
      message,
      severity: c.parameters.length > 0 ? vscode.DiagnosticSeverity.Information : vscode.DiagnosticSeverity.Hint,
      source: DIAGNOSTIC_COLLECTION,
    });
  }

  for (const issue of concreteTypeIssues) {
    diagnostics.push({
      range: new vscode.Range(doc.positionAt(issue.startIndex), doc.positionAt(issue.endIndex)),
      message: `DI: Prefer interface over concrete type "${issue.paramType}".`,
      severity: vscode.DiagnosticSeverity.Warning,
      source: DIAGNOSTIC_COLLECTION,
    });
  }

  for (const issue of circularDependencyIssues) {
    const cycleStr = issue.cycle.join(" → ");
    diagnostics.push({
      range: new vscode.Range(0, 0, 0, 0),
      message: `DI: Possible circular dependency: ${cycleStr}`,
      severity: vscode.DiagnosticSeverity.Warning,
      source: DIAGNOSTIC_COLLECTION,
    });
  }

  for (const issue of missingRegistrationIssues) {
    diagnostics.push({
      range: new vscode.Range(doc.positionAt(issue.startIndex), doc.positionAt(issue.endIndex)),
      message: `DI: No Add*(<${issue.paramType}, ...>) registration found in this file.`,
      severity: vscode.DiagnosticSeverity.Warning,
      source: DIAGNOSTIC_COLLECTION,
    });
  }

  for (const err of errors) {
    diagnostics.push({
      range: new vscode.Range(0, 0, 0, 0),
      message: err,
      severity: vscode.DiagnosticSeverity.Warning,
      source: DIAGNOSTIC_COLLECTION,
    });
  }

  collection.set(uri, diagnostics);
}

export function activate(context: vscode.ExtensionContext) {
  const diagnosticCollection = vscode.languages.createDiagnosticCollection(DIAGNOSTIC_COLLECTION);
  context.subscriptions.push(diagnosticCollection);

  const updateDiagnostics = (doc: vscode.TextDocument) => {
    if (doc.languageId === "csharp") {
      analyzeCSharpDocument(doc, diagnosticCollection);
    }
  };

  const runAnalyzeCommand = vscode.commands.registerCommand(
    "di-plugin.analyzeFile",
    async () => {
      const editor = vscode.window.activeTextEditor;
      if (!editor) {
        vscode.window.showInformationMessage("Open a C# file to analyze.");
        return;
      }

      const doc = editor.document;
      if (doc.languageId !== "csharp") {
        vscode.window.showInformationMessage("This command works on C# files. Current file: " + doc.languageId);
        return;
      }

      const source = doc.getText();
      const {
        constructors,
        errors,
        concreteTypeIssues,
        circularDependencyIssues,
        missingRegistrationIssues,
      } = analyzeCSharp(source);

      outputChannel.clear();
      outputChannel.appendLine(`DI Analysis: ${doc.fileName}`);
      outputChannel.appendLine("");

      if (errors.length > 0) {
        outputChannel.appendLine("Warnings:");
        errors.forEach((e) => outputChannel.appendLine("  - " + e));
        outputChannel.appendLine("");
      }

      if (
        concreteTypeIssues.length > 0 ||
        circularDependencyIssues.length > 0 ||
        missingRegistrationIssues.length > 0
      ) {
        outputChannel.appendLine("DI issues:");
        for (const issue of concreteTypeIssues) {
          outputChannel.appendLine(`  - Concrete type: ${issue.className}(...) has parameter ${issue.paramType} ${issue.paramName} — prefer an interface.`);
        }
        for (const issue of circularDependencyIssues) {
          outputChannel.appendLine(`  - Circular dependency: ${issue.cycle.join(" → ")}`);
        }
        for (const issue of missingRegistrationIssues) {
          outputChannel.appendLine(
            `  - Missing registration: ${issue.paramType} required by ${issue.className}(...) has no Add*(<${issue.paramType}, ...>) call in this file.`
          );
        }
        outputChannel.appendLine("");
      }

      if (constructors.length === 0) {
        outputChannel.appendLine("No constructors found.");
      } else {
        outputChannel.appendLine("Constructors and dependencies:");
        for (const c of constructors) {
          outputChannel.appendLine(`  ${c.className}(...)`);
          for (const p of c.parameters) {
            outputChannel.appendLine(`    - ${p.type} ${p.name}`);
          }
        }
      }

      outputChannel.show();
      analyzeCSharpDocument(doc, diagnosticCollection);
      vscode.window.showInformationMessage(
        `DI: Found ${constructors.length} constructor(s). See Output → "Dependency Injection".`
      );
    }
  );

  const suggestRegistrationsCommand = vscode.commands.registerCommand(
    "di-plugin.suggestRegistrations",
    async () => {
      const editor = vscode.window.activeTextEditor;
      if (!editor) {
        vscode.window.showInformationMessage("Open a C# file to analyze.");
        return;
      }

      const doc = editor.document;
      if (doc.languageId !== "csharp") {
        vscode.window.showInformationMessage(
          "This command works on C# files. Current file: " + doc.languageId
        );
        return;
      }

      const source = doc.getText();
      const { constructors } = analyzeCSharp(source);

      outputChannel.clear();
      outputChannel.appendLine(`DI Registration Suggestions: ${doc.fileName}`);
      outputChannel.appendLine("");

      if (constructors.length === 0) {
        outputChannel.appendLine("No constructors found to suggest registrations for.");
      } else {
        const suggestions = buildRegistrationSuggestions(constructors, source);
        if (suggestions.length === 0) {
          outputChannel.appendLine("No registration suggestions generated.");
        } else {
          outputChannel.appendLine("// Example DI registrations (adjust lifetime as needed):");
          for (const line of suggestions) {
            outputChannel.appendLine("  " + line);
          }
        }
      }

      outputChannel.show();
    }
  );

  const codeActionProvider = vscode.languages.registerCodeActionsProvider(
    "csharp",
    new DiCodeActionProvider(),
    {
      providedCodeActionKinds: [vscode.CodeActionKind.QuickFix],
    }
  );

  context.subscriptions.push(runAnalyzeCommand, suggestRegistrationsCommand, codeActionProvider);

  vscode.workspace.onDidOpenTextDocument(updateDiagnostics);
  vscode.workspace.onDidChangeTextDocument((e) => {
    if (e.document.languageId === "csharp") updateDiagnostics(e.document);
  });
  vscode.workspace.onDidCloseTextDocument((doc) => diagnosticCollection.delete(doc.uri));

  for (const doc of vscode.workspace.textDocuments) {
    if (doc.languageId === "csharp") updateDiagnostics(doc);
  }
}

export function deactivate() {}
