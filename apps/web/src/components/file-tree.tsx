import { FolderIcon, FileIcon } from "lucide-react";
import { ScrollArea } from "@/components/ui/scroll-area";
import { cn } from "@/lib/utils";

interface FileTreeRow {
  path: string;
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
      const path = prefix ? `${prefix}/${entry.name}` : entry.name;
      rows.push({ path, name: entry.name, depth, isFile: entry.isFile });
      if (!entry.isFile) walk(entry, depth + 1, path);
    }
  }
  walk(root, 0, "");
  return rows;
}

export function FileTree({
  paths,
  bordered = true,
  variant = "specification",
  className,
  selectedPath,
  onSelectFile,
}: {
  paths: string[];
  bordered?: boolean;
  variant?: "specification" | "generated";
  className?: string;
  selectedPath?: string | null;
  onSelectFile?: (path: string) => void;
}) {
  const rows = buildRows(paths);
  const isGenerated = variant === "generated";
  return (
    <ScrollArea
      className={cn(
        "overflow-hidden",
        className ?? "max-h-85",
        bordered && "rounded-lg border bg-card",
      )}
    >
      {rows.map((row) => {
        const selected = row.isFile && row.path === selectedPath;
        const Element = row.isFile && onSelectFile ? "button" : "div";
        return (
          <Element
            key={row.path}
            type={Element === "button" ? "button" : undefined}
            onClick={
              row.isFile && onSelectFile
                ? () => onSelectFile(row.path)
                : undefined
            }
            className={`flex w-full items-center gap-2 border-b text-left last:border-b-0 ${
              isGenerated
                ? "px-5 py-[9px] text-[13px] font-sans text-[#3f3f46]"
                : "px-3.5 py-[7px] text-[12.5px] font-mono"
            } ${
              row.isFile && onSelectFile
                ? "cursor-pointer hover:bg-[#f4f4f5]"
                : ""
            } ${selected ? "bg-[#eff6ff] hover:bg-[#eff6ff]" : ""}`}
            style={{
              paddingLeft: `${(isGenerated ? 20 : 14) + row.depth * 18}px`,
            }}
          >
            {row.isFile ? (
              <FileIcon
                className={`${isGenerated ? "size-[15px]" : "size-[13px]"} shrink-0 ${selected ? "text-[#1d4ed8]" : "text-muted-foreground"}`}
              />
            ) : (
              <FolderIcon
                className={`${isGenerated ? "size-[15px]" : "size-[13px]"} shrink-0 text-foreground`}
              />
            )}
            <span
              className={
                row.isFile
                  ? selected
                    ? "font-medium text-[#1d4ed8]"
                    : isGenerated
                      ? "text-[#3f3f46]"
                      : "text-muted-foreground"
                  : "font-semibold"
              }
            >
              {row.name}
            </span>
          </Element>
        );
      })}
    </ScrollArea>
  );
}
