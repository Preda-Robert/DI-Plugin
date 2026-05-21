import * as vscode from "vscode";
import type { WorkspaceFileRef } from "./registrationInsert";

export function interfaceNameForConcreteType(concreteType: string): string {
  if (/^I[A-Z]/.test(concreteType)) return concreteType;
  return `I${concreteType}`;
}

export function interfaceExistsInWorkspace(
  interfaceName: string,
  files: WorkspaceFileRef[]
): boolean {
  const re = new RegExp(`\\binterface\\s+${interfaceName}\\b`);
  return files.some((f) => re.test(f.source));
}

export function findImplementationFile(
  concreteType: string,
  files: WorkspaceFileRef[]
): WorkspaceFileRef | undefined {
  const re = new RegExp(`\\bclass\\s+${concreteType}\\b`);
  return files.find((f) => re.test(f.source));
}

function inferInterfaceFileUri(
  implFile: vscode.Uri,
  interfaceName: string,
  files: WorkspaceFileRef[]
): vscode.Uri {
  const normalized = (p: string) => p.replace(/\\/g, "/");
  const ifaceSample = files
    .map((f) => normalized(f.doc.uri.fsPath))
    .find((p) => /\/interfaces\//i.test(p));
  if (ifaceSample) {
    const dir = ifaceSample.replace(/\/[^/]+$/i, "");
    return vscode.Uri.file(`${dir}/${interfaceName}.cs`);
  }
  const implPath = normalized(implFile.fsPath);
  const dir = implPath.replace(/\/[^/]+$/, "");
  return vscode.Uri.file(`${dir}/${interfaceName}.cs`);
}

function buildInterfaceFileContent(namespace: string | undefined, interfaceName: string): string {
  const lines = [
    namespace ? `namespace ${namespace};` : "",
    "",
    `public interface ${interfaceName}`,
    "{",
    "}",
    "",
  ];
  return lines.filter((l, i) => !(i === 0 && !namespace)).join("\n");
}

export function buildExtractInterfaceWorkspaceEdit(options: {
  concreteType: string;
  consumerClassName: string;
  consumerDoc: vscode.TextDocument;
  paramStartOffset: number;
  paramEndOffset: number;
  files: WorkspaceFileRef[];
}): { edit: vscode.WorkspaceEdit; registrationLine: string } | undefined {
  const { concreteType, consumerClassName, consumerDoc, paramStartOffset, paramEndOffset, files } =
    options;
  const iface = interfaceNameForConcreteType(concreteType);
  const impl = findImplementationFile(concreteType, files);
  if (!impl) return undefined;

  const edit = new vscode.WorkspaceEdit();
  const registrationLine = `services.AddScoped<${iface}, ${concreteType}>();`;

  if (!interfaceExistsInWorkspace(iface, files)) {
    const ns = extractNamespace(impl.source);
    const ifaceUri = inferInterfaceFileUri(impl.doc.uri, iface, files);
    edit.createFile(ifaceUri, { ignoreIfExists: true });
    edit.insert(ifaceUri, new vscode.Position(0, 0), buildInterfaceFileContent(ns, iface));
  }

  const classDecl = new RegExp(
    `(public\\s+class\\s+${concreteType}\\s*:\\s*)([^{\r\n]+)`,
    "m"
  );
  const classMatch = classDecl.exec(impl.source);
  if (classMatch) {
    const bases = classMatch[2];
    if (!bases.split(",").some((b) => b.split("<")[0].trim() === iface)) {
      const newBases = `${bases.trim()}, ${iface}`;
      const start = classMatch.index + classMatch[1].length;
      const end = start + classMatch[2].length;
      edit.replace(
        impl.doc.uri,
        new vscode.Range(impl.doc.positionAt(start), impl.doc.positionAt(end)),
        newBases
      );
    }
  } else {
    const simpleClass = new RegExp(`(public\\s+class\\s+${concreteType}\\s*)(\\{)`, "m");
    const sm = simpleClass.exec(impl.source);
    if (sm) {
      const insertAt = sm.index + sm[1].length;
      edit.insert(impl.doc.uri, impl.doc.positionAt(insertAt), `: ${iface} `);
    }
  }

  const consumerSource = consumerDoc.getText();
  const ctorRe = new RegExp(
    `(public\\s+${consumerClassName}\\s*\\([^)]*?)\\b${concreteType}\\b`,
    "s"
  );
  const ctorMatch = ctorRe.exec(consumerSource);
  if (ctorMatch) {
    const typeStart = ctorMatch.index + ctorMatch[1].length;
    edit.replace(
      consumerDoc.uri,
      new vscode.Range(consumerDoc.positionAt(typeStart), consumerDoc.positionAt(typeStart + concreteType.length)),
      iface
    );
  } else {
    edit.replace(
      consumerDoc.uri,
      new vscode.Range(
        consumerDoc.positionAt(paramStartOffset),
        consumerDoc.positionAt(paramEndOffset)
      ),
      iface
    );
  }

  return { edit, registrationLine };
}

function extractNamespace(source: string): string | undefined {
  const match = source.match(/namespace\s+([A-Za-z0-9_.]+)\s*;/);
  return match ? match[1] : undefined;
}
