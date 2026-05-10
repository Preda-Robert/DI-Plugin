import * as vscode from "vscode";
import { analyzeCSharp, ConstructorInfo } from "./csharp/analyzer";

const DIAGNOSTIC_COLLECTION = "di-plugin";
const outputChannel = vscode.window.createOutputChannel("Dependency Injection");

async function resolveTargetCSharpDocument(): Promise<vscode.TextDocument | undefined> {
  const active = vscode.window.activeTextEditor?.document;
  if (active?.languageId === "csharp") {
    return active;
  }

  const visibleCSharp = vscode.window.visibleTextEditors
    .map((e) => e.document)
    .find((d) => d.languageId === "csharp");
  if (visibleCSharp) {
    return visibleCSharp;
  }

  const openedCSharp = vscode.workspace.textDocuments.find((d) => d.languageId === "csharp");
  if (openedCSharp) {
    return openedCSharp;
  }

  const csFiles = await vscode.workspace.findFiles(
    "**/*.cs",
    "**/{bin,obj,node_modules,.git}/**",
    50
  );
  if (csFiles.length === 0) {
    return undefined;
  }

  const selected = await vscode.window.showQuickPick(
    csFiles.map((uri) => ({
      label: vscode.workspace.asRelativePath(uri),
      description: uri.fsPath,
      uri,
    })),
    {
      title: "Select a C# file for DI analysis",
      placeHolder: "No active C# editor found",
    }
  );
  if (!selected) {
    return undefined;
  }
  return vscode.workspace.openTextDocument(selected.uri);
}

interface WorkspaceCSharpFile {
  doc: vscode.TextDocument;
  source: string;
}

interface WorkspaceDiSummary {
  files: WorkspaceCSharpFile[];
  constructors: Array<ConstructorInfo & { fileName: string }>;
  registeredTypes: Set<string>;
  interfaceToImpl: Map<string, Set<string>>;
  typeToNamespace: Map<string, string>;
  hasCompositionRoot: boolean;
}

interface GeneratedFile {
  fileName: string;
  content: string;
}

function collectInterfaceImplementations(source: string): Map<string, Set<string>> {
  const interfaceToImpl = new Map<string, Set<string>>();
  const classRegex = /public\s+class\s+(\w+)(\s*:\s*([^{\r\n]+))?/g;
  let match: RegExpExecArray | null;
  while ((match = classRegex.exec(source)) !== null) {
    const implName = match[1];
    const bases = match[3];
    if (!bases) continue;

    for (const rawBase of bases.split(",")) {
      const baseName = rawBase.split("<")[0].trim();
      if (!/^I[A-Z]/.test(baseName)) continue;
      if (!interfaceToImpl.has(baseName)) {
        interfaceToImpl.set(baseName, new Set());
      }
      interfaceToImpl.get(baseName)?.add(implName);
    }
  }
  return interfaceToImpl;
}

function collectRegisteredTypes(source: string): Set<string> {
  const registeredTypes = new Set<string>();
  const registrationRegex = /Add(?:Singleton|Scoped|Transient)\s*<([^>]+)>/g;
  let match: RegExpExecArray | null;
  while ((match = registrationRegex.exec(source)) !== null) {
    match[1]
      .split(",")
      .map((t) => t.trim())
      .filter((t) => t.length > 0)
      .forEach((t) => registeredTypes.add(t));
  }
  return registeredTypes;
}

async function getWorkspaceCSharpFiles(): Promise<WorkspaceCSharpFile[]> {
  const uris = await vscode.workspace.findFiles("**/*.cs", "**/{bin,obj,node_modules,.git}/**", 200);
  const docs = await Promise.all(uris.map((uri) => vscode.workspace.openTextDocument(uri)));
  return docs
    .filter((doc) => doc.languageId === "csharp")
    .map((doc) => ({ doc, source: doc.getText() }));
}

async function buildWorkspaceSummary(): Promise<WorkspaceDiSummary> {
  const files = await getWorkspaceCSharpFiles();
  const constructors: Array<ConstructorInfo & { fileName: string }> = [];
  const registeredTypes = new Set<string>();
  const interfaceToImpl = new Map<string, Set<string>>();
  const typeToNamespace = new Map<string, string>();
  let hasCompositionRoot = false;

  for (const file of files) {
    const analysis = analyzeCSharp(file.source);
    for (const c of analysis.constructors) {
      constructors.push({ ...c, fileName: file.doc.fileName });
    }
    for (const type of collectRegisteredTypes(file.source)) {
      registeredTypes.add(type);
    }
    for (const [iface, impls] of collectInterfaceImplementations(file.source)) {
      if (!interfaceToImpl.has(iface)) {
        interfaceToImpl.set(iface, new Set());
      }
      for (const impl of impls) {
        interfaceToImpl.get(iface)?.add(impl);
      }
    }

    const ns = extractNamespace(file.source);
    if (ns) {
      const typeRegex = /(?:public\s+)?(?:class|interface)\s+([A-Za-z_][A-Za-z0-9_]*)/g;
      let typeMatch: RegExpExecArray | null;
      while ((typeMatch = typeRegex.exec(file.source)) !== null) {
        typeToNamespace.set(typeMatch[1], ns);
      }
    }
    if (/\bclass\s+CompositionRoot\b/.test(file.source)) {
      hasCompositionRoot = true;
    }
  }

  return {
    files,
    constructors,
    registeredTypes,
    interfaceToImpl,
    typeToNamespace,
    hasCompositionRoot,
  };
}

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
  const interfaceToImpl = collectInterfaceImplementations(source);

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

function buildFactorySuggestions(constructors: ConstructorInfo[], source: string): string[] {
  const suggestions = new Set<string>();
  const interfaceToImpl = collectInterfaceImplementations(source);
  const interfaceNames = new Set(interfaceToImpl.keys());

  for (const c of constructors) {
    if (!c.className || c.parameters.length === 0) continue;
    const args = c.parameters.map((p) => `${p.type} ${p.name}`).join(", ");
    const invokeArgs = c.parameters.map((p) => p.name).join(", ");

    if (c.parameters.length >= 2 || c.parameters.some((p) => interfaceNames.has(p.type))) {
      suggestions.add(`public sealed class ${c.className}Factory { public ${c.className} Create(${args}) => new ${c.className}(${invokeArgs}); }`);
    }

    if (/Builder$/.test(c.className) || c.parameters.length >= 3) {
      suggestions.add(`public sealed class ${c.className}Builder { /* capture dependencies, then Build() */ public ${c.className} Build(${args}) => new ${c.className}(${invokeArgs}); }`);
    }
  }

  return Array.from(suggestions).sort();
}

function extractNamespace(source: string): string | undefined {
  const match = source.match(/namespace\s+([A-Za-z0-9_.]+)/);
  return match ? match[1] : undefined;
}

function getBestNamespace(summary: WorkspaceDiSummary): string {
  const counts = new Map<string, number>();
  for (const file of summary.files) {
    const ns = extractNamespace(file.source);
    if (!ns) continue;
    counts.set(ns, (counts.get(ns) ?? 0) + 1);
  }
  let best = "GeneratedDI";
  let bestCount = -1;
  for (const [ns, count] of counts.entries()) {
    if (count > bestCount) {
      best = ns;
      bestCount = count;
    }
  }
  return `${best}.DI`;
}

function shouldGenerateFactory(c: ConstructorInfo, interfaceNames: Set<string>): boolean {
  if (!c.className || c.parameters.length === 0) return false;
  // More conservative: generate only for genuinely complex DI services.
  return c.parameters.length >= 3 && c.parameters.every((p) => interfaceNames.has(p.type));
}

function getRequiredUsingsForTypes(types: string[], summary: WorkspaceDiSummary): string[] {
  const lines: string[] = [];
  const seen = new Set<string>();
  for (const t of types) {
    const ns = summary.typeToNamespace.get(t);
    if (ns && !seen.has(ns)) {
      seen.add(ns);
      lines.push(ns);
    }
  }
  return lines.sort();
}

function buildFactoryFileContent(c: ConstructorInfo, namespaceName: string, summary: WorkspaceDiSummary): string {
  const invokeArgs = c.parameters
    .map((p) => `_provider.GetRequiredService<${p.type}>()`)
    .join(", ");
  const useNs = getRequiredUsingsForTypes(
    [c.className, ...c.parameters.map((p) => p.type)],
    summary
  );
  const lines: string[] = [
    "using System;",
    "using Microsoft.Extensions.DependencyInjection;",
  ];
  for (const ns of useNs) {
    lines.push(`using ${ns};`);
  }
  lines.push("");
  lines.push(`namespace ${namespaceName}.Generated`);
  lines.push("{");
  lines.push(`    public sealed class ${c.className}Factory`);
  lines.push("    {");
  lines.push("        private readonly IServiceProvider _provider;");
  lines.push("");
  lines.push(`        public ${c.className}Factory(IServiceProvider provider)`);
  lines.push("        {");
  lines.push("            _provider = provider;");
  lines.push("        }");
  lines.push("");
  lines.push(`        public ${c.className} Create() => new ${c.className}(${invokeArgs});`);
  lines.push("    }");
  lines.push("}");
  lines.push("");
  return lines.join("\n");
}

function buildAppBuilderFileContent(
  summary: WorkspaceDiSummary,
  namespaceName: string,
  factoryTargets: ConstructorInfo[]
): string {
  const lines: string[] = [
    "using System;",
    "using Microsoft.Extensions.DependencyInjection;",
  ];
  const allTypes = new Set<string>();
  for (const c of summary.constructors) {
    if (c.className) allTypes.add(c.className);
    for (const p of c.parameters) allTypes.add(p.type);
  }
  for (const ns of getRequiredUsingsForTypes(Array.from(allTypes), summary)) {
    lines.push(`using ${ns};`);
  }
  lines.push("");
  lines.push(`namespace ${namespaceName}.Generated`);
  lines.push("{");
  lines.push("    public static class AppBuilder");
  lines.push("    {");
  lines.push("        // NOTE: Generated AppBuilder is intended to replace/supersede manual CompositionRoot wiring.");
  lines.push("        public static IServiceProvider Build()");
  lines.push("        {");
  lines.push("            var services = new ServiceCollection();");
  const registrations: string[] = [];
  for (const [iface, impls] of summary.interfaceToImpl.entries()) {
    for (const impl of impls) {
      registrations.push(`            services.AddScoped<${iface}, ${impl}>();`);
    }
  }
  for (const c of summary.constructors) {
    if (!c.className) continue;
    registrations.push(`            services.AddScoped<${c.className}>();`);
  }
  for (const c of factoryTargets) {
    registrations.push(`            services.AddScoped<${c.className}Factory>();`);
  }
  for (const line of Array.from(new Set(registrations)).sort()) lines.push(line);
  lines.push("            return services.BuildServiceProvider();");
  lines.push("        }");
  lines.push("    }");
  lines.push("}");
  lines.push("");
  return lines.join("\n");
}

function buildWorkspaceGeneratedFiles(summary: WorkspaceDiSummary): GeneratedFile[] {
  const namespaceName = getBestNamespace(summary);
  const generated: GeneratedFile[] = [];
  const interfaceNames = new Set(summary.interfaceToImpl.keys());
  const factoryTargets: ConstructorInfo[] = [];

  for (const c of summary.constructors) {
    if (!shouldGenerateFactory(c, interfaceNames)) continue;
    factoryTargets.push(c);
    generated.push({
      fileName: `${c.className}Factory.cs`,
      content: buildFactoryFileContent(c, namespaceName, summary),
    });
  }

  generated.push({
    fileName: "AppBuilder.cs",
    content: buildAppBuilderFileContent(summary, namespaceName, factoryTargets),
  });
  return generated;
}

function buildWorkspaceFactorySuggestions(summary: WorkspaceDiSummary): string[] {
  const files = buildWorkspaceGeneratedFiles(summary);
  const suggestions: string[] = [];
  for (const file of files) {
    suggestions.push(`// ${file.fileName}`);
    suggestions.push(file.content.trimEnd());
    suggestions.push("");
  }
  return suggestions;
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
      const doc = await resolveTargetCSharpDocument();
      if (!doc) {
        vscode.window.showInformationMessage(
          "No C# file found. Open a .cs file or select one when prompted."
        );
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
      const doc = await resolveTargetCSharpDocument();
      if (!doc) {
        vscode.window.showInformationMessage(
          "No C# file found. Open a .cs file or select one when prompted."
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

  const analyzeWorkspaceCommand = vscode.commands.registerCommand(
    "di-plugin.analyzeWorkspace",
    async () => {
      const summary = await buildWorkspaceSummary();

      outputChannel.clear();
      outputChannel.appendLine(`DI Workspace Analysis`);
      outputChannel.appendLine("");

      if (summary.files.length === 0) {
        outputChannel.appendLine("No C# files found in workspace.");
        outputChannel.show();
        return;
      }

      outputChannel.appendLine(`C# files scanned: ${summary.files.length}`);
      outputChannel.appendLine(`Constructors found: ${summary.constructors.length}`);
      outputChannel.appendLine("");

      const primitiveTypes = new Set([
        "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
        "float", "double", "decimal", "char", "string", "object",
      ]);

      const missing: string[] = [];
      for (const c of summary.constructors) {
        for (const p of c.parameters) {
          if (!p.type || p.type === "?" || primitiveTypes.has(p.type)) continue;
          if (!summary.registeredTypes.has(p.type)) {
            missing.push(`${p.type} required by ${c.className}(...) in ${vscode.workspace.asRelativePath(c.fileName)}`);
          }
        }
      }

      if (missing.length > 0) {
        outputChannel.appendLine("Missing registrations across workspace:");
        for (const item of missing.sort()) {
          outputChannel.appendLine(`  - ${item}`);
        }
        outputChannel.appendLine("");
      }

      outputChannel.appendLine("Constructors by file:");
      for (const c of summary.constructors) {
        outputChannel.appendLine(`  ${vscode.workspace.asRelativePath(c.fileName)} :: ${c.className}(...)`);
      }
      outputChannel.show();
    }
  );

  const suggestWorkspaceRegistrationsCommand = vscode.commands.registerCommand(
    "di-plugin.suggestWorkspaceRegistrations",
    async () => {
      const summary = await buildWorkspaceSummary();
      outputChannel.clear();
      outputChannel.appendLine("DI Workspace Registration Suggestions");
      outputChannel.appendLine("");

      if (summary.files.length === 0) {
        outputChannel.appendLine("No C# files found in workspace.");
        outputChannel.show();
        return;
      }

      const suggestions = new Set<string>();
      for (const [iface, impls] of summary.interfaceToImpl.entries()) {
        for (const impl of impls) {
          suggestions.add(`services.AddScoped<${iface}, ${impl}>();`);
        }
      }
      for (const c of summary.constructors) {
        if (!c.className) continue;
        const covered = Array.from(summary.interfaceToImpl.values()).some((impls) => impls.has(c.className));
        if (!covered) {
          suggestions.add(`services.AddTransient<${c.className}>();`);
        }
      }

      for (const line of Array.from(suggestions).sort()) {
        outputChannel.appendLine(`  ${line}`);
      }
      outputChannel.show();
    }
  );

  const suggestFactoriesCommand = vscode.commands.registerCommand(
    "di-plugin.suggestFactories",
    async () => {
      const doc = await resolveTargetCSharpDocument();
      if (!doc) {
        vscode.window.showInformationMessage(
          "No C# file found. Open a .cs file or select one when prompted."
        );
        return;
      }

      const source = doc.getText();
      const { constructors } = analyzeCSharp(source);
      let suggestions = buildFactorySuggestions(constructors, source);

      // If current file has no constructor-based suggestions, fallback to workspace-wide suggestions.
      if (suggestions.length === 0) {
        const summary = await buildWorkspaceSummary();
        suggestions = buildWorkspaceFactorySuggestions(summary);
      }

      outputChannel.clear();
      outputChannel.appendLine(`DI Factory/Builder Suggestions: ${doc.fileName}`);
      outputChannel.appendLine("");

      if (suggestions.length === 0) {
        outputChannel.appendLine("No factory or builder suggestions generated.");
      } else {
        for (const suggestion of suggestions) {
          outputChannel.appendLine(suggestion);
          outputChannel.appendLine("");
        }
      }
      outputChannel.show();
    }
  );

  const suggestWorkspaceFactoriesCommand = vscode.commands.registerCommand(
    "di-plugin.suggestWorkspaceFactories",
    async () => {
      const summary = await buildWorkspaceSummary();
      outputChannel.clear();
      outputChannel.appendLine("DI Workspace Builder/Factory Suggestions");
      outputChannel.appendLine("");

      if (summary.files.length === 0) {
        outputChannel.appendLine("No C# files found in workspace.");
        outputChannel.show();
        return;
      }

      const suggestions = buildWorkspaceFactorySuggestions(summary);
      if (suggestions.length === 0) {
        outputChannel.appendLine("No factory or builder suggestions generated.");
      } else {
        if (summary.hasCompositionRoot) {
          outputChannel.appendLine("Note: Existing CompositionRoot detected; generated AppBuilder is a replacement candidate.");
          outputChannel.appendLine("");
        }
        for (const line of suggestions) {
          outputChannel.appendLine(line);
        }
      }
      outputChannel.show();
    }
  );

  const generateWorkspaceFactoriesCommand = vscode.commands.registerCommand(
    "di-plugin.generateWorkspaceFactories",
    async () => {
      const summary = await buildWorkspaceSummary();
      if (summary.files.length === 0) {
        vscode.window.showInformationMessage("No C# files found in workspace.");
        return;
      }

      const workspaceFolder = vscode.workspace.workspaceFolders?.[0];
      if (!workspaceFolder) {
        vscode.window.showInformationMessage("Open a workspace folder to generate files.");
        return;
      }

      const generatedFiles = buildWorkspaceGeneratedFiles(summary);
      if (generatedFiles.length === 0) {
        vscode.window.showInformationMessage("No builder/factory files to generate.");
        return;
      }

      const targetDir = vscode.Uri.joinPath(workspaceFolder.uri, "GeneratedDI");
      await vscode.workspace.fs.createDirectory(targetDir);
      const encoder = new TextEncoder();

      for (const file of generatedFiles) {
        const targetFile = vscode.Uri.joinPath(targetDir, file.fileName);
        await vscode.workspace.fs.writeFile(targetFile, encoder.encode(file.content));
      }

      outputChannel.clear();
      outputChannel.appendLine("DI Generated Builder/Factory Files");
      outputChannel.appendLine("");
      outputChannel.appendLine(`Target folder: ${vscode.workspace.asRelativePath(targetDir)}`);
      outputChannel.appendLine(`Files generated: ${generatedFiles.length}`);
      if (summary.hasCompositionRoot) {
        outputChannel.appendLine("Note: Existing CompositionRoot detected; generated AppBuilder is intended to supersede it.");
      }
      for (const file of generatedFiles) {
        outputChannel.appendLine(`  - ${vscode.workspace.asRelativePath(vscode.Uri.joinPath(targetDir, file.fileName))}`);
      }
      outputChannel.show();
      vscode.window.showInformationMessage(
        `DI: Generated ${generatedFiles.length} file(s) in ${vscode.workspace.asRelativePath(targetDir)}`
      );
    }
  );

  const codeActionProvider = vscode.languages.registerCodeActionsProvider(
    "csharp",
    new DiCodeActionProvider(),
    {
      providedCodeActionKinds: [vscode.CodeActionKind.QuickFix],
    }
  );

  context.subscriptions.push(
    runAnalyzeCommand,
    suggestRegistrationsCommand,
    analyzeWorkspaceCommand,
    suggestWorkspaceRegistrationsCommand,
    suggestFactoriesCommand,
    suggestWorkspaceFactoriesCommand,
    generateWorkspaceFactoriesCommand,
    codeActionProvider
  );

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
