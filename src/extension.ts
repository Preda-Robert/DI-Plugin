import * as vscode from "vscode";
import { analyzeCSharp, ConstructorInfo } from "./csharp/analyzer";
import {
  buildExtractInterfaceWorkspaceEdit,
  interfaceNameForConcreteType,
} from "./extractInterface";
import {
  createRegistrationInsertEdit,
  detectRegistrationLifetime,
  filterNewRegistrationLines,
  findGeneratedAppBuilderFile,
  findRegistrationInsertTarget,
  normalizeRegistrationLifetime,
  parseRegistrationsFromAppBuilder,
} from "./registrationInsert";

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
  constructors: Array<ConstructorInfo & { fileName: string; relativePath: string }>;
  registeredTypes: Set<string>;
  interfaceToImpl: Map<string, Set<string>>;
  typeToNamespace: Map<string, string>;
  hasCompositionRoot: boolean;
  /** True if workspace looks like ASP.NET Core MVC (controllers resolved by framework). */
  aspNetMvcHost: boolean;
  /** Class base names that are generic (e.g. `Repository` from `Repository<T>`) — do not register as closed types. */
  genericClassNames: Set<string>;
  /** Classes that are config/mapping/EF artifacts, not injectable application services. */
  nonInjectableClassNames: Set<string>;
}

/** Framework/marker interfaces — implemented alongside service contracts but not DI keys. */
const MARKER_SERVICE_INTERFACES = new Set([
  "IDisposable",
  "IAsyncDisposable",
  "ICloneable",
  "ISerializable",
  "INotifyPropertyChanged",
  "INotifyPropertyChanging",
]);

function isMarkerServiceInterface(interfaceName: string): boolean {
  const base = interfaceName.split("<")[0].trim();
  return MARKER_SERVICE_INTERFACES.has(base);
}

function shouldSuggestInterfaceRegistration(interfaceName: string): boolean {
  return !isMarkerServiceInterface(interfaceName);
}

/** Base types that indicate a class is not registered via Add*<T>() directly. */
function isNonInjectableBaseType(baseName: string): boolean {
  const base = baseName.split("<")[0].trim();
  return (
    base === "Profile" ||
    base === "Migration" ||
    base === "DbContext" ||
    base === "IdentityDbContext" ||
    base === "Controller" ||
    base === "ControllerBase" ||
    base === "PageModel" ||
    base === "RazorPage" ||
    base === "ViewComponent"
  );
}

function collectNonInjectableClassNames(source: string): Set<string> {
  const names = new Set<string>();
  const classRegex = /public\s+class\s+(\w+)(\s*:\s*([^{\r\n]+))?/g;
  let match: RegExpExecArray | null;
  while ((match = classRegex.exec(source)) !== null) {
    const className = match[1];
    const bases = match[3];
    if (!bases) continue;
    for (const rawBase of bases.split(",")) {
      const baseName = rawBase.split("<")[0].trim();
      if (isNonInjectableBaseType(baseName)) {
        names.add(className);
        break;
      }
    }
  }
  return names;
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
      if (!shouldSuggestInterfaceRegistration(baseName)) continue;
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

/** Merge DI registration hints from typical ASP.NET Core + MS.Ext.DI patterns. */
function collectExtendedRegistrationTypes(source: string): Set<string> {
  const types = collectRegisteredTypes(source);

  // builder.Services / services — same patterns as collectRegisteredTypes already scanned

  // AddDbContext<ApplicationDbContext>(...) registers context + options
  const addDb = /AddDbContext\s*<\s*([A-Za-z0-9_]+)\s*>/g;
  let dbMatch: RegExpExecArray | null;
  while ((dbMatch = addDb.exec(source)) !== null) {
    const ctx = dbMatch[1].trim();
    types.add(ctx);
    types.add(`DbContextOptions<${ctx}>`);
  }

  // new ServiceDescriptor(typeof(IThing), typeof(Thing), ...)
  const sd =
    /typeof\s*\(\s*([A-Za-z0-9_]+)\s*\)\s*,\s*typeof\s*\(\s*([A-Za-z0-9_]+)\s*\)/gs;
  let sdMatch: RegExpExecArray | null;
  while ((sdMatch = sd.exec(source)) !== null) {
    types.add(sdMatch[1].trim());
    types.add(sdMatch[2].trim());
  }

  // AddIdentity<TUser, TRole>() registers identity managers for TUser / TRole
  const idMatch = source.match(/AddIdentity\s*<\s*([^,>]+)\s*,\s*([^>]+)\s*>/);
  if (idMatch) {
    const user = idMatch[1].trim();
    const role = idMatch[2].trim();
    types.add(`UserManager<${user}>`);
    types.add(`SignInManager<${user}>`);
    types.add(`RoleManager<${role}>`);
  }

  return types;
}

function documentRelativePath(uri: vscode.Uri): string {
  const folder = vscode.workspace.workspaceFolders?.[0];
  if (!folder) return uri.fsPath;
  return vscode.workspace.asRelativePath(uri, false);
}

function isLikelyTestPath(relativePath: string): boolean {
  const p = relativePath.replace(/\\/g, "/").toLowerCase();
  if (p.includes("/tests/")) return true;
  const base = p.split("/").pop() ?? "";
  return /tests\.cs$/i.test(base) || /_tests\.cs$/i.test(base);
}

function isLikelyMvcControllerClass(className: string): boolean {
  return className.endsWith("Controller");
}

function isImplicitFrameworkDependency(paramType: string, summary: WorkspaceDiSummary): boolean {
  const t = paramType.trim();
  if (t === "IWebHostEnvironment" || t === "IHostEnvironment") return true;
  if (t === "IHostApplicationLifetime" || t === "IConfiguration") return true;
  if (t === "ILogger" || t.startsWith("ILogger<")) return true;
  if (t.startsWith("IOptions<") || t.startsWith("IOptionsSnapshot<")) return true;
  if (summary.aspNetMvcHost && isLikelyMvcControllerClass(t)) return false;
  return false;
}

function isRegisteredOrImplicit(paramType: string, summary: WorkspaceDiSummary): boolean {
  if (summary.registeredTypes.has(paramType)) return true;
  return isImplicitFrameworkDependency(paramType, summary);
}

function shouldSuggestConcreteRegistration(
  className: string,
  relativePath: string,
  summary: WorkspaceDiSummary
): boolean {
  if (!className) return false;
  if (summary.genericClassNames.has(className)) return false;
  if (isLikelyTestPath(relativePath)) return false;
  if (/Tests$/i.test(className)) return false;
  if (summary.aspNetMvcHost && isLikelyMvcControllerClass(className)) return false;
  if (summary.nonInjectableClassNames.has(className)) return false;
  return true;
}

function shouldAnalyzeConstructorForMissing(
  className: string,
  relativePath: string,
  summary: WorkspaceDiSummary
): boolean {
  if (isLikelyTestPath(relativePath)) return false;
  if (/Tests$/i.test(className)) return false;
  if (summary.aspNetMvcHost && isLikelyMvcControllerClass(className)) return false;
  return true;
}

async function getWorkspaceCSharpFiles(): Promise<WorkspaceCSharpFile[]> {
  const uris = await vscode.workspace.findFiles("**/*.cs", "**/{bin,obj,node_modules,.git}/**", 200);
  const files: WorkspaceCSharpFile[] = [];
  for (const uri of uris) {
    try {
      const doc = await vscode.workspace.openTextDocument(uri);
      if (doc.languageId !== "csharp") continue;
      files.push({ doc, source: doc.getText() });
    } catch {
      // Skip unreadable paths (e.g. invalid characters on some platforms).
    }
  }
  return files;
}

async function buildWorkspaceSummary(): Promise<WorkspaceDiSummary> {
  const files = await getWorkspaceCSharpFiles();
  const constructors: Array<ConstructorInfo & { fileName: string; relativePath: string }> = [];
  const registeredTypes = new Set<string>();
  const interfaceToImpl = new Map<string, Set<string>>();
  const typeToNamespace = new Map<string, string>();
  let hasCompositionRoot = false;
  let aspNetMvcHost = false;
  const genericClassNames = new Set<string>();
  const nonInjectableClassNames = new Set<string>();

  for (const file of files) {
    const relativePath = documentRelativePath(file.doc.uri);
    const src = file.source;
    const genMatch = /\bclass\s+(\w+)\s*</g;
    let gm: RegExpExecArray | null;
    while ((gm = genMatch.exec(src)) !== null) {
      genericClassNames.add(gm[1]);
    }
    const analysis = analyzeCSharp(file.source);
    for (const c of analysis.constructors) {
      constructors.push({ ...c, fileName: file.doc.fileName, relativePath });
    }
    for (const type of collectExtendedRegistrationTypes(file.source)) {
      registeredTypes.add(type);
    }
    if (
      /\bWebApplication\.CreateBuilder\b/.test(src) ||
      /\bAddControllersWithViews\s*\(/.test(src) ||
      /\bAddControllers\s*\(/.test(src)
    ) {
      aspNetMvcHost = true;
    }
    for (const [iface, impls] of collectInterfaceImplementations(file.source)) {
      if (!interfaceToImpl.has(iface)) {
        interfaceToImpl.set(iface, new Set());
      }
      for (const impl of impls) {
        interfaceToImpl.get(iface)?.add(impl);
      }
    }
    for (const name of collectNonInjectableClassNames(file.source)) {
      nonInjectableClassNames.add(name);
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
    aspNetMvcHost,
    genericClassNames,
    nonInjectableClassNames,
  };
}

function registrationLifetimeForSummary(
  summary: WorkspaceDiSummary,
  compositionRootSource?: string
): "Scoped" | "Transient" | "Singleton" {
  if (compositionRootSource) {
    return detectRegistrationLifetime(compositionRootSource);
  }
  return summary.aspNetMvcHost ? "Scoped" : "Transient";
}

function buildRegistrationLineForMissingType(
  typeName: string,
  summary: WorkspaceDiSummary,
  compositionRootSource?: string
): string | undefined {
  const lifetime = registrationLifetimeForSummary(summary, compositionRootSource);

  if (/^I[A-Z]/.test(typeName)) {
    if (summary.registeredTypes.has(typeName)) return undefined;
    for (const [iface, impls] of summary.interfaceToImpl) {
      if (iface === typeName && impls.size > 0) {
        const impl = [...impls][0];
        return `services.Add${lifetime}<${iface}, ${impl}>();`;
      }
    }
    return `services.Add${lifetime}<${typeName}>();`;
  }

  // Concrete type: register I{Name} → Type even when Type appears in another mapping (e.g. IEmailSender → SmtpEmailSender).
  const iface = interfaceNameForConcreteType(typeName);
  if (summary.registeredTypes.has(iface)) return undefined;
  return `services.Add${lifetime}<${iface}, ${typeName}>();`;
}

function createRegistrationEdit(
  doc: vscode.TextDocument,
  typeName: string,
  summary?: WorkspaceDiSummary
): vscode.WorkspaceEdit | undefined {
  const line =
    summary !== undefined
      ? buildRegistrationLineForMissingType(typeName, summary)
      : `services.AddTransient<${typeName}>();`;
  if (!line) return undefined;

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
  edit.insert(doc.uri, insertPos, `\n            ${line}`);
  return edit;
}

async function insertRegistrationsIntoWorkspace(
  registrationLines: string[]
): Promise<boolean> {
  const files = await getWorkspaceCSharpFiles();
  const target = findRegistrationInsertTarget(files);
  if (!target) {
    vscode.window.showWarningMessage(
      "DI: No CompositionRoot, Add*Services, or ConfigureServices method found to insert registrations."
    );
    return false;
  }

  const targetFile = files.find((f) => f.doc.uri.toString() === target.uri.toString());
  if (!targetFile) return false;

  const preferredLifetime = detectRegistrationLifetime(targetFile.source);
  const normalized = normalizeRegistrationLifetime(registrationLines, preferredLifetime);
  const newLines = filterNewRegistrationLines(targetFile.source, normalized);
  if (newLines.length === 0) {
    vscode.window.showInformationMessage("DI: All registrations are already present in the composition root.");
    return false;
  }

  const edit = createRegistrationInsertEdit(target, newLines);
  if (!edit) return false;

  const applied = await vscode.workspace.applyEdit(edit);
  if (applied) {
    const doc = await vscode.workspace.openTextDocument(target.uri);
    await vscode.window.showTextDocument(doc);
    vscode.window.showInformationMessage(
      `DI: Inserted ${newLines.length} registration(s) into ${target.label}.`
    );
  }
  return applied;
}

function buildRegistrationSuggestions(constructors: ConstructorInfo[], source: string): string[] {
  const suggestions = new Set<string>();
  const interfaceToImpl = collectInterfaceImplementations(source);
  const nonInjectable = collectNonInjectableClassNames(source);

  // 1) Suggest interface-to-implementation registrations where we see "class Impl : IFace"
  for (const [iface, impls] of interfaceToImpl.entries()) {
    if (!shouldSuggestInterfaceRegistration(iface)) continue;
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
    if (nonInjectable.has(type)) continue;
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
  const rootCounts = new Map<string, number>();
  for (const file of summary.files) {
    const ns = extractNamespace(file.source);
    if (!ns) continue;
    const root = ns.split(".")[0];
    rootCounts.set(root, (rootCounts.get(root) ?? 0) + 1);
  }
  let bestRoot = "GeneratedDI";
  let bestCount = -1;
  for (const [root, count] of rootCounts.entries()) {
    if (count > bestCount) {
      bestRoot = root;
      bestCount = count;
    }
  }
  return `${bestRoot}.DI`;
}

function isInTestNamespace(typeName: string, summary: WorkspaceDiSummary): boolean {
  const ns = summary.typeToNamespace.get(typeName);
  return ns ? /\.Tests(\.|$)/i.test(ns) : false;
}

function isImplementationOfMappedInterface(className: string, summary: WorkspaceDiSummary): boolean {
  for (const impls of summary.interfaceToImpl.values()) {
    if (impls.has(className)) return true;
  }
  return false;
}

function buildWorkspaceRegistrationSuggestions(
  summary: WorkspaceDiSummary,
  preferredLifetime?: "Scoped" | "Transient" | "Singleton"
): string[] {
  const lifetime =
    preferredLifetime ?? (summary.aspNetMvcHost ? "Scoped" : "Transient");
  const suggestions = new Set<string>();
  for (const [iface, impls] of summary.interfaceToImpl.entries()) {
    if (!shouldSuggestInterfaceRegistration(iface)) continue;
    for (const impl of impls) {
      if (isInTestNamespace(impl, summary)) continue;
      if (summary.genericClassNames.has(impl)) continue;
      if (summary.registeredTypes.has(iface)) continue;
      suggestions.add(`services.Add${lifetime}<${iface}, ${impl}>();`);
    }
  }
  for (const c of summary.constructors) {
    if (!c.className) continue;
    const ext = c as ConstructorInfo & { fileName: string; relativePath: string };
    if (!shouldSuggestConcreteRegistration(c.className, ext.relativePath, summary)) continue;
    if (isImplementationOfMappedInterface(c.className, summary)) continue;
    if (summary.registeredTypes.has(c.className)) continue;
    suggestions.add(`services.Add${lifetime}<${c.className}>();`);
  }
  return Array.from(suggestions).sort();
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

/** Pull simple type tokens from `...<A, B>...` fragments in generated registration lines. */
function extractTypesFromRegistrationLines(registrationLines: string[]): Set<string> {
  const types = new Set<string>();
  for (const line of registrationLines) {
    const re = /<([^>]+)>/g;
    let m: RegExpExecArray | null;
    while ((m = re.exec(line)) !== null) {
      for (const part of m[1].split(",")) {
        const t = part.trim();
        if (t) types.add(t);
      }
    }
  }
  return types;
}

function buildAppBuilderFileContent(
  summary: WorkspaceDiSummary,
  namespaceName: string,
  factoryTargets: ConstructorInfo[]
): string {
  const registrations: string[] = [];
  for (const [iface, impls] of summary.interfaceToImpl.entries()) {
    if (!shouldSuggestInterfaceRegistration(iface)) continue;
    for (const impl of impls) {
      if (isInTestNamespace(impl, summary)) continue;
      if (summary.genericClassNames.has(impl)) continue;
      if (summary.registeredTypes.has(iface)) continue;
      registrations.push(`            services.AddScoped<${iface}, ${impl}>();`);
    }
  }
  for (const c of summary.constructors) {
    if (!c.className) continue;
    if (!shouldSuggestConcreteRegistration(c.className, c.relativePath, summary)) continue;
    if (summary.registeredTypes.has(c.className)) continue;
    if (isImplementationOfMappedInterface(c.className, summary)) continue;
    registrations.push(`            services.AddScoped<${c.className}>();`);
  }
  for (const c of factoryTargets) {
    registrations.push(`            services.AddScoped<${c.className}Factory>();`);
  }

  const uniqueRegs = Array.from(new Set(registrations)).sort();

  const typesForUsings = extractTypesFromRegistrationLines(uniqueRegs);
  for (const c of factoryTargets) {
    if (c.className) typesForUsings.add(c.className);
    for (const p of c.parameters) typesForUsings.add(p.type);
  }

  const lines: string[] = [
    "using System;",
    "using Microsoft.Extensions.DependencyInjection;",
  ];
  for (const ns of getRequiredUsingsForTypes(Array.from(typesForUsings), summary)) {
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
  if (uniqueRegs.length === 0 && factoryTargets.length === 0) {
    lines.push(
      "            // No inferred DI registrations to add (workspace already wired or nothing matched heuristics)."
    );
  }
  for (const line of uniqueRegs) {
    lines.push(line);
  }
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
    const ext = c as ConstructorInfo & { fileName: string; relativePath: string };
    if (!shouldSuggestConcreteRegistration(ext.className, ext.relativePath, summary)) continue;
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

function rangesOverlap(a: vscode.Range, b: vscode.Range): boolean {
  return !a.isEmpty && !b.isEmpty && a.intersection(b) !== undefined;
}

class DiCodeActionProvider implements vscode.CodeActionProvider {
  provideCodeActions(
    document: vscode.TextDocument,
    _range: vscode.Range,
    context: vscode.CodeActionContext
  ): vscode.ProviderResult<vscode.CodeAction[]> {
    const actions: vscode.CodeAction[] = [];
    const { concreteTypeIssues } = analyzeCSharp(document.getText());

    for (const diag of context.diagnostics) {
      if (diag.source !== DIAGNOSTIC_COLLECTION) continue;

      if (diag.message.startsWith("DI: No Add*(")) {
        const match = diag.message.match(/<([^,>]+),/);
        const paramType = match ? match[1].trim() : undefined;
        if (!paramType) continue;

        const localEdit = createRegistrationEdit(document, paramType);
        if (localEdit) {
          const action = new vscode.CodeAction(
            `Add DI registration for ${paramType} (this file)`,
            vscode.CodeActionKind.QuickFix
          );
          action.diagnostics = [diag];
          action.edit = localEdit;
          actions.push(action);
        }

        const workspaceAction = new vscode.CodeAction(
          `Insert workspace DI registration for ${paramType}`,
          vscode.CodeActionKind.QuickFix
        );
        workspaceAction.diagnostics = [diag];
        workspaceAction.command = {
          command: "di-plugin.insertRegistrationForType",
          title: `Insert workspace registration for ${paramType}`,
          arguments: [paramType],
        };
        actions.push(workspaceAction);
      }

      if (diag.message.includes("Prefer interface over concrete type")) {
        const typeMatch = diag.message.match(/concrete type "([^"]+)"/);
        const concreteType = typeMatch?.[1];
        if (!concreteType) continue;

        const issue = concreteTypeIssues.find((i) =>
          rangesOverlap(
            diag.range,
            new vscode.Range(
              safePosition(document, i.startIndex),
              safePosition(document, i.endIndex)
            )
          )
        );
        if (!issue) continue;

        const iface = interfaceNameForConcreteType(concreteType);
        const registerAction = new vscode.CodeAction(
          `Register ${iface} → ${concreteType} in composition root`,
          vscode.CodeActionKind.QuickFix
        );
        registerAction.diagnostics = [diag];
        registerAction.command = {
          command: "di-plugin.insertRegistrationForType",
          title: `Register ${iface} → ${concreteType}`,
          arguments: [concreteType],
        };
        actions.push(registerAction);

        const extractAction = new vscode.CodeAction(
          `Extract ${iface} and register`,
          vscode.CodeActionKind.QuickFix
        );
        extractAction.diagnostics = [diag];
        extractAction.command = {
          command: "di-plugin.extractInterfaceAndRegister",
          title: `Extract ${iface} and register`,
          arguments: [
            document.uri.toString(),
            concreteType,
            issue.className,
            issue.startIndex,
            issue.endIndex,
          ],
        };
        actions.push(extractAction);
      }
    }

    return actions;
  }
}

function safePosition(doc: vscode.TextDocument, offset: number): vscode.Position {
  const len = doc.getText().length;
  return doc.positionAt(Math.max(0, Math.min(offset, len)));
}

function analyzeCSharpDocument(
  doc: vscode.TextDocument,
  collection: vscode.DiagnosticCollection
): void {
  if (doc.languageId !== "csharp") return;

  const uri = doc.uri;
  const diagnostics: vscode.Diagnostic[] = [];

  const source = doc.getText();
  let analysis: ReturnType<typeof analyzeCSharp>;
  try {
    analysis = analyzeCSharp(source);
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    collection.set(uri, [
      {
        range: new vscode.Range(0, 0, 0, 0),
        message: `DI: Analysis failed: ${msg}`,
        severity: vscode.DiagnosticSeverity.Warning,
        source: DIAGNOSTIC_COLLECTION,
      },
    ]);
    return;
  }
  const { constructors, errors, concreteTypeIssues, circularDependencyIssues, missingRegistrationIssues } =
    analysis;

  for (const err of errors) {
    diagnostics.push({
      range: new vscode.Range(0, 0, 0, 0),
      message: err,
      severity: vscode.DiagnosticSeverity.Warning,
      source: DIAGNOSTIC_COLLECTION,
    });
  }

  for (const c of constructors) {
    const range = new vscode.Range(safePosition(doc, c.startIndex), safePosition(doc, c.endIndex));
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
      range: new vscode.Range(safePosition(doc, issue.startIndex), safePosition(doc, issue.endIndex)),
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
      range: new vscode.Range(safePosition(doc, issue.startIndex), safePosition(doc, issue.endIndex)),
      message: `DI: No Add*(<${issue.paramType}, ...>) registration found in this file.`,
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
      if (summary.aspNetMvcHost) {
        outputChannel.appendLine(
          "ASP.NET Core hosting detected: missing-registration checks skip MVC controllers and common framework services (ILogger<>, IWebHostEnvironment, Identity managers when AddIdentity is present, DbContext when AddDbContext is present)."
        );
      }
      outputChannel.appendLine(
        "Test projects (paths containing /Tests/ or *Tests.cs) are excluded from missing-registration analysis."
      );
      outputChannel.appendLine("");

      const primitiveTypes = new Set([
        "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
        "float", "double", "decimal", "char", "string", "object",
      ]);

      const missing: string[] = [];
      for (const c of summary.constructors) {
        const ext = c as ConstructorInfo & { fileName: string; relativePath: string };
        if (!shouldSuggestConcreteRegistration(c.className, ext.relativePath, summary)) continue;
        for (const p of c.parameters) {
          if (!p.type || p.type === "?" || primitiveTypes.has(p.type)) continue;
          if (isRegisteredOrImplicit(p.type, summary)) continue;
          missing.push(
            `${p.type} required by ${c.className}(...) in ${vscode.workspace.asRelativePath(c.fileName)}`
          );
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

      const suggestions = buildWorkspaceRegistrationSuggestions(summary);

      if (suggestions.length === 0) {
        outputChannel.appendLine(
          "  (No lines to suggest: registrations in Program/Startup already cover inferred bindings, or nothing matched heuristics.)"
        );
      } else {
        for (const line of suggestions) {
          outputChannel.appendLine(`  ${line}`);
        }
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

  const insertWorkspaceRegistrationsCommand = vscode.commands.registerCommand(
    "di-plugin.insertWorkspaceRegistrations",
    async () => {
      const summary = await buildWorkspaceSummary();
      const files = await getWorkspaceCSharpFiles();
      const target = findRegistrationInsertTarget(files);
      const targetSource = target
        ? files.find((f) => f.doc.uri.toString() === target.uri.toString())?.source
        : undefined;
      const preferredLifetime = targetSource
        ? detectRegistrationLifetime(targetSource)
        : undefined;
      const lines = buildWorkspaceRegistrationSuggestions(summary, preferredLifetime);
      if (lines.length === 0) {
        vscode.window.showInformationMessage(
          "DI: No missing registrations to insert (workspace appears fully wired)."
        );
        return;
      }
      await insertRegistrationsIntoWorkspace(lines);
    }
  );

  const insertRegistrationForTypeCommand = vscode.commands.registerCommand(
    "di-plugin.insertRegistrationForType",
    async (typeName: string) => {
      if (!typeName) return;
      const summary = await buildWorkspaceSummary();
      const files = await getWorkspaceCSharpFiles();
      const target = findRegistrationInsertTarget(files);
      const targetSource = target
        ? files.find((f) => f.doc.uri.toString() === target.uri.toString())?.source
        : undefined;
      const line = buildRegistrationLineForMissingType(typeName, summary, targetSource);
      if (!line) {
        vscode.window.showInformationMessage(`DI: ${typeName} is already registered.`);
        return;
      }
      await insertRegistrationsIntoWorkspace([line]);
    }
  );

  const mergeGeneratedIntoCompositionRootCommand = vscode.commands.registerCommand(
    "di-plugin.mergeGeneratedIntoCompositionRoot",
    async () => {
      const folder = vscode.workspace.workspaceFolders?.[0];
      if (!folder) {
        vscode.window.showInformationMessage("Open a workspace folder first.");
        return;
      }

      const appBuilderUri = await findGeneratedAppBuilderFile(folder);
      if (!appBuilderUri) {
        vscode.window.showWarningMessage(
          "DI: No GeneratedDI/AppBuilder.cs found. Run “Generate builders/factories files (workspace)” first."
        );
        return;
      }

      const appBuilderDoc = await vscode.workspace.openTextDocument(appBuilderUri);
      const lines = parseRegistrationsFromAppBuilder(appBuilderDoc.getText());
      if (lines.length === 0) {
        vscode.window.showWarningMessage("DI: AppBuilder.cs contains no services.Add* lines to merge.");
        return;
      }

      await insertRegistrationsIntoWorkspace(lines);
    }
  );

  const extractInterfaceAndRegisterCommand = vscode.commands.registerCommand(
    "di-plugin.extractInterfaceAndRegister",
    async (
      docUri: string,
      concreteType: string,
      consumerClassName: string,
      paramStart: number,
      paramEnd: number
    ) => {
      const consumerDoc = await vscode.workspace.openTextDocument(vscode.Uri.parse(docUri));
      const files = await getWorkspaceCSharpFiles();
      const built = buildExtractInterfaceWorkspaceEdit({
        concreteType,
        consumerClassName,
        consumerDoc,
        paramStartOffset: paramStart,
        paramEndOffset: paramEnd,
        files,
      });
      if (!built) {
        vscode.window.showWarningMessage(
          `DI: Could not extract interface for ${concreteType} (implementation class not found).`
        );
        return;
      }

      const applied = await vscode.workspace.applyEdit(built.edit);
      if (!applied) return;

      await insertRegistrationsIntoWorkspace([built.registrationLine]);
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
    insertWorkspaceRegistrationsCommand,
    insertRegistrationForTypeCommand,
    mergeGeneratedIntoCompositionRootCommand,
    extractInterfaceAndRegisterCommand,
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
