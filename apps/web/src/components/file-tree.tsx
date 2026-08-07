import { FolderIcon, FileIcon } from "lucide-react";

interface FileTreeRow {
  key: string;
  name: string;
  depth: number;
  isFile: boolean;
}

function buildRows(paths: string[]): FileTreeRow[] {
  interface Node {
    name: string;
    isFile: boolean;
    children: Map<string, Node>;
  }
  const root: Node = { name: "", isFile: false, children: new Map() };

  for (const path of paths) {
    const parts = path.split("/").filter(Boolean);
    let node = root;
    parts.forEach((part, i) => {
      const isFile = i === parts.length - 1;
      if (!node.children.has(part)) {
        node.children.set(part, { name: part, isFile, children: new Map() });
      }
      node = node.children.get(part)!;
    });
  }

  const rows: FileTreeRow[] = [];
  function walk(node: Node, depth: number, prefix: string) {
    for (const entry of node.children.values()) {
      const key = `${prefix}/${entry.name}`;
      rows.push({ key, name: entry.name, depth, isFile: entry.isFile });
      if (!entry.isFile) walk(entry, depth + 1, key);
    }
  }
  walk(root, 0, "");
  return rows;
}

export function FileTree({ paths, bordered = true }: { paths: string[]; bordered?: boolean }) {
  const rows = buildRows(paths);
  return (
    <div className={`max-h-[340px] overflow-y-auto ${bordered ? "rounded-lg border bg-card" : ""}`}>
      {rows.map((row) => (
        <div
          key={row.key}
          className="flex items-center gap-2 px-3.5 py-[7px] border-b last:border-b-0 text-[12.5px] font-mono"
          style={{ paddingLeft: `${14 + row.depth * 18}px` }}
        >
          {row.isFile ? (
            <FileIcon className="size-[13px] shrink-0 text-muted-foreground" />
          ) : (
            <FolderIcon className="size-[13px] shrink-0 text-foreground" />
          )}
          <span className={row.isFile ? "text-muted-foreground" : "font-semibold"}>{row.name}</span>
        </div>
      ))}
    </div>
  );
}
