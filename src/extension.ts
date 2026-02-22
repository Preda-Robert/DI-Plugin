import * as vscode from "vscode";
import { analyzeCSharp } from "./csharp/analyzer";

const DIAGNOSTIC_COLLECTION = "di-plugin";
const outputChannel = vscode.window.createOutputChannel("Dependency Injection");

function analyzeCSharpDocument(
  doc: vscode.TextDocument,
  collection: vscode.DiagnosticCollection
): void {
  if (doc.languageId !== "csharp") return;

  const uri = doc.uri;
  const diagnostics: vscode.Diagnostic[] = [];

  const source = doc.getText();
  const { constructors, errors, concreteTypeIssues, circularDependencyIssues } = analyzeCSharp(source);

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
      const { constructors, errors, concreteTypeIssues, circularDependencyIssues } = analyzeCSharp(source);

      outputChannel.clear();
      outputChannel.appendLine(`DI Analysis: ${doc.fileName}`);
      outputChannel.appendLine("");

      if (errors.length > 0) {
        outputChannel.appendLine("Warnings:");
        errors.forEach((e) => outputChannel.appendLine("  - " + e));
        outputChannel.appendLine("");
      }

      if (concreteTypeIssues.length > 0 || circularDependencyIssues.length > 0) {
        outputChannel.appendLine("DI issues:");
        for (const issue of concreteTypeIssues) {
          outputChannel.appendLine(`  - Concrete type: ${issue.className}(...) has parameter ${issue.paramType} ${issue.paramName} — prefer an interface.`);
        }
        for (const issue of circularDependencyIssues) {
          outputChannel.appendLine(`  - Circular dependency: ${issue.cycle.join(" → ")}`);
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

  context.subscriptions.push(runAnalyzeCommand);

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
