import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { FolderIcon, FileIcon } from "lucide-react";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { DownloadButton } from "@/components/download-button";
import { FileTree } from "@/components/file-tree";
import { PageLoading } from "@/components/page-loading";
import { NotFoundView } from "@/components/not-found-view";
import { Card, CardHeader } from "@/components/ui/card";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Spinner } from "@/components/ui/spinner";
import { buttonVariants } from "@/components/ui/button";
import { useSpecification } from "@/hooks/use-specifications";
import { useGeneration, useGenerationFile } from "@/hooks/use-generated";
import { ApiError } from "@/lib/api-client";
import { cn } from "@/lib/utils";

function pickDefaultPath(fileTree: string[]): string | null {
  return (
    fileTree.find((path) => path.toLowerCase().endsWith("readme.md")) ??
    fileTree[0] ??
    null
  );
}

function FileContentPreview({
  generationId,
  path,
}: {
  generationId: string;
  path: string | null;
}) {
  const fileQuery = useGenerationFile(generationId, path);

  if (!path) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <FileIcon className="size-4" />
        Select a file to preview
      </div>
    );
  }

  return (
    <>
      <CardHeader className="flex items-center gap-2 rounded-none border-b bg-[#fafafa] px-5 py-3 text-xs font-semibold text-muted-foreground">
        <FileIcon className="size-3.5" />
        <span className="font-mono">{path}</span>
      </CardHeader>
      <ScrollArea className="min-h-0 flex-1">
        {fileQuery.isPending && (
          <div className="flex items-center gap-2 p-6 text-sm text-muted-foreground">
            <Spinner />
            Loading…
          </div>
        )}
        {fileQuery.isError && (
          <div className="p-6 text-sm text-destructive">
            {fileQuery.error instanceof ApiError
              ? fileQuery.error.message
              : "Couldn't load this file."}
          </div>
        )}
        {fileQuery.data && (
          <pre className="p-5 text-[12.5px] font-mono leading-relaxed whitespace-pre-wrap text-foreground/80">
            {fileQuery.data.content}
          </pre>
        )}
      </ScrollArea>
    </>
  );
}

export function VersionFilesPage() {
  const { id, version } = useParams<{ id: string; version: string }>();
  const versionNumber = Number(version);

  const specQuery = useSpecification(id);
  const summary = specQuery.data?.generations.find(
    (g) => g.version === versionNumber,
  );
  const generationQuery = useGeneration(summary?.id);
  const generation = generationQuery.data;

  const [selectedPath, setSelectedPath] = useState<string | null>(null);

  useEffect(() => {
    if (generation) setSelectedPath(pickDefaultPath(generation.fileTree));
  }, [generation]);

  if (specQuery.isPending || (summary && generationQuery.isPending))
    return <PageLoading />;
  if (specQuery.isError) {
    if (specQuery.error instanceof ApiError && specQuery.error.status === 404) {
      return <NotFoundView message="This project doesn't exist." />;
    }
    throw specQuery.error;
  }
  if (!summary || !generation)
    return <NotFoundView message="This version doesn't exist." />;

  const spec = specQuery.data;

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="flex h-[calc(100vh-3.5rem-5rem)] w-full max-w-350 flex-col">
        <PageBreadcrumb
          items={[
            { label: "Generated Projects", href: "/generated" },
            { label: spec.name, href: "/generated" },
            { label: `v${generation.version}` },
          ]}
        />

        <div className="flex items-start justify-between gap-4 mb-5.5">
          <div>
            <h1 className="text-[22px] font-bold tracking-tight">
              {spec.name}
            </h1>
            <p className="text-[13px] text-muted-foreground mt-1">
              Generated files — v{generation.version} · {generation.model}
            </p>
          </div>
          <div className="flex gap-2 shrink-0">
            <DownloadButton
              path={`/api/generations/${generation.id}/download`}
              filename={`${spec.name}-v${generation.version}.zip`}
            />
            <Link
              to={`/specifications/${spec.id}`}
              className={cn(buttonVariants({ variant: "outline" }))}
            >
              View specification →
            </Link>
          </div>
        </div>

        <div className="grid min-h-0 flex-1 grid-cols-[300px_1fr] gap-4">
          <Card className="flex min-h-0 flex-col rounded-[12px] p-0 overflow-hidden">
            <CardHeader className="flex items-center gap-2 rounded-none border-b bg-[#fafafa] px-5 py-3 text-xs font-semibold text-muted-foreground">
              <FolderIcon className="size-3.5" />
              Generated project files
            </CardHeader>
            <FileTree
              paths={generation.fileTree}
              bordered={false}
              variant="generated"
              className="min-h-0 flex-1"
              selectedPath={selectedPath}
              onSelectFile={setSelectedPath}
            />
          </Card>

          <Card className="flex min-h-0 flex-col rounded-[12px] p-0 overflow-hidden">
            <FileContentPreview
              generationId={generation.id}
              path={selectedPath}
            />
          </Card>
        </div>
      </div>
    </main>
  );
}
