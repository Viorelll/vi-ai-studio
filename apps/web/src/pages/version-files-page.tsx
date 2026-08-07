import { Link, useParams } from "react-router-dom";
import { FolderIcon } from "lucide-react";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { FileTree } from "@/components/file-tree";
import { PageLoading } from "@/components/page-loading";
import { NotFoundView } from "@/components/not-found-view";
import { Card } from "@/components/ui/card";
import { buttonVariants } from "@/components/ui/button";
import { useSpecification } from "@/hooks/use-specifications";
import { useGeneration } from "@/hooks/use-generated";
import { API_BASE_URL, ApiError } from "@/lib/api-client";
import { cn } from "@/lib/utils";

export function VersionFilesPage() {
  const { id, version } = useParams<{ id: string; version: string }>();
  const versionNumber = Number(version);

  const specQuery = useSpecification(id);
  const summary = specQuery.data?.generations.find((g) => g.version === versionNumber);
  const generationQuery = useGeneration(summary?.id);

  if (specQuery.isPending || (summary && generationQuery.isPending)) return <PageLoading />;
  if (specQuery.isError) {
    if (specQuery.error instanceof ApiError && specQuery.error.status === 404) {
      return <NotFoundView message="This project doesn't exist." />;
    }
    throw specQuery.error;
  }
  if (!summary || !generationQuery.data) return <NotFoundView message="This version doesn't exist." />;

  const spec = specQuery.data;
  const generation = generationQuery.data;

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[820px]">
        <PageBreadcrumb
          items={[
            { label: "Generated Projects", href: "/generated" },
            { label: spec.name, href: "/generated" },
            { label: `v${generation.version}` },
          ]}
        />

        <div className="flex items-start justify-between gap-4 mb-5.5">
          <div>
            <h1 className="text-[22px] font-bold tracking-tight">{spec.name}</h1>
            <p className="text-[13px] text-muted-foreground mt-1">
              Generated files — v{generation.version} · {generation.model}
            </p>
          </div>
          <div className="flex gap-2 shrink-0">
            <a href={`${API_BASE_URL}/api/generations/${generation.id}/download`} className={cn(buttonVariants())}>
              Download .zip
            </a>
            <Link to={`/specifications/${spec.id}`} className={cn(buttonVariants({ variant: "outline" }))}>
              View specification →
            </Link>
          </div>
        </div>

        <Card className="p-0 overflow-hidden">
          <div className="flex items-center gap-2 px-5 py-3 border-b bg-muted/40 text-xs font-semibold text-muted-foreground">
            <FolderIcon className="size-3.5" />
            Generated project files
          </div>
          <FileTree paths={generation.fileTree} bordered={false} />
        </Card>
      </div>
    </main>
  );
}
