import * as vscode from "vscode";

export interface WorkspaceFileRef {
  doc: vscode.TextDocument;
  source: string;
}

export interface RegistrationInsertTarget {
  uri: vscode.Uri;
  insertPosition: vscode.Position;
  indent: string;
  label: string;
}

/** Find where to insert `services.Add*` lines (CompositionRoot, Add*Services, or ConfigureServices). */
export function findRegistrationInsertTarget(
  files: WorkspaceFileRef[]
): RegistrationInsertTarget | undefined {
  for (const file of files) {
    if (/\bclass\s+CompositionRoot\b/.test(file.source)) {
      const point = findInsertBeforeServicesReturn(file.source, file.doc);
      if (point) {
        return { uri: file.doc.uri, label: "CompositionRoot", ...point };
      }
    }
  }

  for (const file of files) {
    const methodMatch = file.source.match(
      /public\s+static\s+IServiceCollection\s+(Add\w+Services)\s*\(/
    );
    if (!methodMatch) continue;
    const point = findInsertBeforeServicesReturn(file.source, file.doc);
    if (point) {
      return { uri: file.doc.uri, label: methodMatch[1], ...point };
    }
  }

  for (const file of files) {
    if (!/void\s+ConfigureServices\s*\(\s*IServiceCollection\s+services\s*\)/.test(file.source)) {
      continue;
    }
    const point = findInsertBeforeServicesReturn(file.source, file.doc);
    if (point) {
      return { uri: file.doc.uri, label: "ConfigureServices", ...point };
    }
  }

  return undefined;
}

function findInsertBeforeServicesReturn(
  source: string,
  doc: vscode.TextDocument
): Pick<RegistrationInsertTarget, "insertPosition" | "indent"> | undefined {
  const returnRegex = /return\s+services\s*(?:\.\s*BuildServiceProvider\s*\(\s*\))?\s*;/g;
  let match: RegExpExecArray | null;
  let last: RegExpExecArray | null = null;
  while ((match = returnRegex.exec(source)) !== null) {
    last = match;
  }
  if (!last) return undefined;

  // Insert at the start of the `return` line (not at `return`), so existing indentation is preserved.
  let lineStart = last.index;
  while (lineStart > 0 && source[lineStart - 1] !== "\n" && source[lineStart - 1] !== "\r") {
    lineStart--;
  }
  const insertPosition = doc.positionAt(lineStart);
  const indent = detectServicesAddIndent(source) ?? "            ";
  return { insertPosition, indent };
}

function detectServicesAddIndent(source: string): string | undefined {
  const m = source.match(/^(\s+)services\.Add\w+/m);
  return m?.[1];
}

export function filterNewRegistrationLines(existingSource: string, lines: string[]): string[] {
  const normalizedExisting = existingSource.replace(/\s+/g, " ");
  return lines.filter((line) => {
    const key = line.trim().replace(/\s+/g, " ");
    return !normalizedExisting.includes(key);
  });
}

export function createRegistrationInsertEdit(
  target: RegistrationInsertTarget,
  lines: string[]
): vscode.WorkspaceEdit | undefined {
  if (lines.length === 0) return undefined;

  const body = lines.map((line) => `${target.indent}${line.trim()}\n`).join("");
  const edit = new vscode.WorkspaceEdit();
  edit.insert(target.uri, target.insertPosition, body);
  return edit;
}

/** Read `services.Add*` lines from generated AppBuilder.cs. */
export function parseRegistrationsFromAppBuilder(source: string): string[] {
  const lines: string[] = [];
  for (const raw of source.split(/\r?\n/)) {
    const trimmed = raw.trim();
    if (!/^services\.Add(?:Singleton|Scoped|Transient)/.test(trimmed)) continue;
    lines.push(trimmed.endsWith(";") ? trimmed : `${trimmed};`);
  }
  return lines;
}

/** Match lifetime already used in a composition root file. */
export function detectRegistrationLifetime(
  compositionRootSource: string
): "Scoped" | "Transient" | "Singleton" {
  const counts = { Scoped: 0, Transient: 0, Singleton: 0 };
  const re = /services\.Add(Scoped|Transient|Singleton)</g;
  let m: RegExpExecArray | null;
  while ((m = re.exec(compositionRootSource)) !== null) {
    counts[m[1] as keyof typeof counts]++;
  }
  if (counts.Scoped >= counts.Transient && counts.Scoped >= counts.Singleton) return "Scoped";
  if (counts.Transient >= counts.Singleton) return "Transient";
  if (counts.Singleton > 0) return "Singleton";
  return "Scoped";
}

/** Rewrite Add* lines to use the composition root's preferred lifetime. */
export function normalizeRegistrationLifetime(
  lines: string[],
  lifetime: "Scoped" | "Transient" | "Singleton"
): string[] {
  return lines.map((line) =>
    line.replace(
      /services\.Add(?:Singleton|Scoped|Transient)/,
      `services.Add${lifetime}`
    )
  );
}

export async function findGeneratedAppBuilderFile(
  workspaceFolder: vscode.WorkspaceFolder
): Promise<vscode.Uri | undefined> {
  const matches = await vscode.workspace.findFiles(
    new vscode.RelativePattern(workspaceFolder, "**/GeneratedDI/AppBuilder.cs"),
    "**/{bin,obj,node_modules,.git}/**",
    1
  );
  return matches[0];
}
