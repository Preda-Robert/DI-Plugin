import Parser from "tree-sitter";
import CSharp from "tree-sitter-c-sharp";

export interface ConstructorParam {
  type: string;
  name: string;
  startIndex: number;
  endIndex: number;
}

export interface ConstructorInfo {
  className: string;
  parameters: ConstructorParam[];
  startIndex: number;
  endIndex: number;
}

/** Constructor parameter is a concrete class instead of an interface (DI smell). */
export interface ConcreteTypeIssue {
  className: string;
  paramType: string;
  paramName: string;
  startIndex: number;
  endIndex: number;
}

/** Circular dependency chain: e.g. ["OrderService", "PaymentService", "OrderService"]. */
export interface CircularDependencyIssue {
  cycle: string[];
}

export interface CSharpDiAnalysis {
  constructors: ConstructorInfo[];
  errors: string[];
  /** Parameter types that are concrete classes in this file (should prefer interfaces). */
  concreteTypeIssues: ConcreteTypeIssue[];
  /** Cycles in constructor dependency graph within this file. */
  circularDependencyIssues: CircularDependencyIssue[];
}

const parser = new Parser();
parser.setLanguage(CSharp);

function getText(source: string, node: Parser.SyntaxNode): string {
  return source.slice(node.startIndex, node.endIndex).trim();
}

function findTypeOfParameter(source: string, paramNode: Parser.SyntaxNode): string {
  // parameter: optional(_parameter_type_with_modifiers), field('name', identifier), ...
  const typeMod = paramNode.childForFieldName("type") ?? paramNode.children.find(
    (c: Parser.SyntaxNode) => c.type === "identifier" || c.type === "generic_name" || c.type === "nullable_type" || c.type === "predefined_type"
  );
  if (typeMod) return getText(source, typeMod);

  // Walk children: first identifier-like before 'name' is usually the type
  const nameNode = paramNode.childForFieldName("name");
  const nameIndex = nameNode ? nameNode.startIndex : paramNode.endIndex;
  for (let i = 0; i < paramNode.childCount; i++) {
    const c = paramNode.child(i);
    if (!c) continue;
    if (c.endIndex <= nameIndex && (c.type === "identifier" || c.type === "generic_name" || c.type === "nullable_type" || c.type === "predefined_type" || c.type === "array_type")) {
      return getText(source, c);
    }
  }
  return "";
}

function collectInterfacesAndClasses(source: string, root: Parser.SyntaxNode): { interfaces: Set<string>; classes: Set<string> } {
  const interfaces = new Set<string>();
  const classes = new Set<string>();

  const collectByType = (type: string, set: Set<string>) => {
    const nodes =
      typeof root.descendantsOfType === "function"
        ? root.descendantsOfType(type)
        : (function collect(node: Parser.SyntaxNode, acc: Parser.SyntaxNode[]): Parser.SyntaxNode[] {
            if (node.type === type) acc.push(node);
            for (let i = 0; i < node.childCount; i++) {
              const c = node.child(i);
              if (c) collect(c, acc);
            }
            return acc;
          })(root, []);
    for (const node of nodes) {
      const nameNode = node.childForFieldName("name");
      if (nameNode) set.add(getText(source, nameNode));
    }
  };
  collectByType("interface_declaration", interfaces);
  collectByType("class_declaration", classes);
  return { interfaces, classes };
}

/** Build map: class name -> list of dependency type names (only types that are classes in this file). */
function buildDependencyGraph(
  constructors: ConstructorInfo[],
  classesInFile: Set<string>
): Map<string, string[]> {
  const graph = new Map<string, string[]>();
  for (const c of constructors) {
    const deps = c.parameters
      .map((p) => p.type)
      .filter((t) => classesInFile.has(t));
    if (deps.length > 0) graph.set(c.className, deps);
  }
  return graph;
}

/** Find cycles in the dependency graph; returns one representative cycle per cycle found. */
function findCycles(graph: Map<string, string[]>): string[][] {
  const cycles: string[][] = [];
  const seenCycles = new Set<string>();

  function dfs(node: string, path: string[], pathSet: Set<string>, visited: Set<string>) {
    if (pathSet.has(node)) {
      const idx = path.indexOf(node);
      if (idx >= 0) {
        const cycle = [...path.slice(idx), node];
        const key = cycle.slice(0, -1).sort().join(",");
        if (!seenCycles.has(key)) {
          seenCycles.add(key);
          cycles.push(cycle);
        }
      }
      return;
    }
    if (visited.has(node)) return;
    visited.add(node);
    path.push(node);
    pathSet.add(node);

    for (const d of graph.get(node) ?? []) {
      dfs(d, path, pathSet, visited);
    }

    path.pop();
    pathSet.delete(node);
  }

  for (const node of graph.keys()) {
    dfs(node, [], new Set(), new Set());
  }
  return cycles;
}

export function analyzeCSharp(source: string): CSharpDiAnalysis {
  const errors: string[] = [];
  const tree = parser.parse(source);
  const constructors: ConstructorInfo[] = [];

  if (tree.rootNode.hasError) {
    errors.push("Parse had syntax errors; analysis may be incomplete.");
  }

  const { interfaces, classes: classesInFile } = collectInterfacesAndClasses(source, tree.rootNode);

  function nodeTypeMatches(node: Parser.SyntaxNode, type: string): boolean {
    return node.type === type || (node as Parser.SyntaxNode & { grammarType?: string }).grammarType === type;
  }

  function collectDescendants(node: Parser.SyntaxNode, type: string): Parser.SyntaxNode[] {
    const out: Parser.SyntaxNode[] = [];
    if (nodeTypeMatches(node, type)) out.push(node);
    for (let i = 0; i < node.childCount; i++) {
      const c = node.child(i);
      if (c) out.push(...collectDescendants(c, type));
    }
    return out;
  }

  // Use tree-sitter's descendantsOfType so we find classes/constructors regardless of nesting
  const classNodes =
    typeof tree.rootNode.descendantsOfType === "function"
      ? tree.rootNode.descendantsOfType("class_declaration")
      : collectDescendants(tree.rootNode, "class_declaration");

  for (const classNode of classNodes) {
    const nameNode = classNode.childForFieldName("name");
    const className = nameNode ? getText(source, nameNode) : "";

    const constructorNodes =
      typeof classNode.descendantsOfType === "function"
        ? (classNode.descendantsOfType("constructor_declaration") as Parser.SyntaxNode[])
        : collectDescendants(classNode, "constructor_declaration");

    for (const child of constructorNodes) {
      const paramList = child.childForFieldName("parameters");
      const parameters: ConstructorParam[] = [];
      if (paramList) {
        // Use namedChildren to get only parameter nodes (not '(' or ',')
        const paramNodes =
          paramList.namedChildCount > 0
            ? Array.from({ length: paramList.namedChildCount }, (_, j) => paramList.namedChild(j)).filter(Boolean) as Parser.SyntaxNode[]
            : Array.from({ length: paramList.childCount }, (_, j) => paramList.child(j)).filter((p): p is Parser.SyntaxNode => p != null && (p.type === "parameter" || p.type === "_parameter_array"));
        for (const p of paramNodes) {
          if (p.type !== "parameter" && p.type !== "_parameter_array") continue;
          const nameNode = p.childForFieldName("name");
          const name = nameNode ? getText(source, nameNode) : "";
          const type = findTypeOfParameter(source, p);
          if (name) {
            parameters.push({
              type: type || "?",
              name,
              startIndex: p.startIndex,
              endIndex: p.endIndex,
            });
          }
        }
      }
      constructors.push({
        className,
        parameters,
        startIndex: child.startIndex,
        endIndex: child.endIndex,
      });
    }
  }

  // DI issues: concrete types and circular dependencies
  const concreteTypeIssues: ConcreteTypeIssue[] = [];
  for (const c of constructors) {
    for (const p of c.parameters) {
      const t = p.type;
      if (!t || t === "?") continue;
      if (classesInFile.has(t) && !interfaces.has(t)) {
        concreteTypeIssues.push({
          className: c.className,
          paramType: t,
          paramName: p.name,
          startIndex: p.startIndex,
          endIndex: p.endIndex,
        });
      }
    }
  }

  const graph = buildDependencyGraph(constructors, classesInFile);
  const cycleLists = findCycles(graph);
  const circularDependencyIssues: CircularDependencyIssue[] = cycleLists.map((cycle) => ({ cycle }));

  return {
    constructors,
    errors,
    concreteTypeIssues,
    circularDependencyIssues,
  };
}
