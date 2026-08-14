import { type ReactNode, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { CircleAlertIcon, FileIcon, FolderIcon, Trash2Icon } from "lucide-react";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { DownloadButton } from "@/components/download-button";
import { FileTree } from "@/components/file-tree";
import { PageLoading } from "@/components/page-loading";
import { NotFoundView } from "@/components/not-found-view";
import { Badge } from "@/components/ui/badge";
import { Card, CardHeader } from "@/components/ui/card";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Spinner } from "@/components/ui/spinner";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogMedia,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  useDeleteSpecification,
  useSpecification,
  useSpecificationDocument,
  useSpecificationDocuments,
} from "@/hooks/use-specifications";
import { ApiError } from "@/lib/api-client";
import { formatDate } from "@/lib/format";
import { getStatusBadgeClassName, getStatusLabel } from "@/lib/status";
import { cn } from "@/lib/utils";

function pickDefaultPath(paths: string[]): string | null {
  return (
    paths.find((path) => path.toLowerCase().endsWith("overview.md")) ??
    paths[0] ??
    null
  );
}

function SpecFileContentPreview({
  specId,
  path,
}: {
  specId: string;
  path: string | null;
}) {
  const fileQuery = useSpecificationDocument(specId, path);

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

export function SpecificationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const specQuery = useSpecification(id);
  const documentsQuery = useSpecificationDocuments(id);
  const deleteSpecification = useDeleteSpecification();
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [selectedPath, setSelectedPath] = useState<string | null>(null);

  useEffect(() => {
    if (documentsQuery.data) setSelectedPath(pickDefaultPath(documentsQuery.data));
  }, [documentsQuery.data]);

  if (specQuery.isPending) return <PageLoading />;
  if (specQuery.isError) {
    if (specQuery.error instanceof ApiError && specQuery.error.status === 404) {
      return <NotFoundView message="This specification doesn't exist." />;
    }
    throw specQuery.error;
  }
  const spec = specQuery.data;

  function confirmDelete() {
    if (!id) return;
    deleteSpecification.mutate(id, {
      onSuccess: () => navigate("/specifications"),
    });
  }

  const stackChips = [
    spec.stack.backend,
    spec.stack.ui,
    spec.stack.database,
    spec.stack.infra,
  ];
  const canEdit = spec.status === "draft" || spec.status === "building" || spec.status === "failed";
  const generationReady = spec.status === "ready";
  const latestGeneration = spec.generations[0]; // API orders these newest-first

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="flex w-full max-w-275 flex-col pb-10">
        <PageBreadcrumb
          items={[
            { label: "Project Specifications", href: "/specifications" },
            { label: spec.name },
          ]}
        />

        <div className="flex items-start justify-between gap-3 mb-1.5">
          <h1 className="text-[22px] font-bold tracking-tight">{spec.name}</h1>
          <div className="flex items-center gap-2 shrink-0">
            <Badge
              variant="outline"
              className={getStatusBadgeClassName(spec.status)}
            >
              {getStatusLabel(spec.status)}
            </Badge>
            {latestGeneration && (
              <Badge
                variant="outline"
                className={getStatusBadgeClassName(latestGeneration.status)}
              >
                Build: {getStatusLabel(latestGeneration.status)}
              </Badge>
            )}
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="text-destructive hover:text-destructive"
              onClick={() => setConfirmingDelete(true)}
            >
              <Trash2Icon />
              Delete
            </Button>
          </div>
        </div>
        <p className="text-sm text-muted-foreground mb-7">
          {spec.summary || "No summary provided."}
        </p>

        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
          <MetaTile label="Owner" value={spec.owner} />
          <MetaTile label="Created" value={formatDate(spec.created)} />
          <MetaTile label="Progress" value={`${spec.progress}%`} />
          <MetaTile label="Audience" value={spec.audience || "Not specified"} />
        </div>

        <Card className="rounded-[12px] p-6 gap-5 mb-5 shrink-0">
          <Section title="Tech stack">
            <div className="flex gap-1.5 flex-wrap">
              {stackChips.map((tech) => (
                <Badge key={tech} variant="secondary" className="font-normal">
                  {tech}
                </Badge>
              ))}
            </div>
          </Section>
        </Card>

        <div className="flex flex-col gap-2">
          <div className="flex shrink-0 items-center justify-between gap-2">
            <span className="flex items-center gap-2 text-xs font-semibold text-muted-foreground">
              <FolderIcon className="size-3.5" />
              Specifications (.md)
              <span className="font-normal">
                ·{" "}
                {documentsQuery.data
                  ? `${documentsQuery.data.length} files`
                  : "…"}
              </span>
            </span>
            <DownloadButton
              path={`/api/specifications/${spec.id}/download`}
              filename={`${spec.name}-specification.zip`}
              variant="outline"
              size="sm"
            />
          </div>

          {/* Sized to show ~20 files at once (row height ~33px) without scrolling the whole page inside a cramped box. */}
          <div className="grid h-175 grid-cols-[280px_1fr] gap-4">
            <Card className="flex min-h-0 flex-col rounded-[12px] p-0 overflow-hidden">
              {documentsQuery.isPending && (
                <div className="flex items-center gap-2 p-5 text-sm text-muted-foreground">
                  <Spinner />
                  Loading files…
                </div>
              )}
              {documentsQuery.isError && (
                <div className="p-5 text-sm text-destructive">
                  Couldn't load the file list.
                </div>
              )}
              {documentsQuery.data && (
                <FileTree
                  paths={documentsQuery.data}
                  bordered={false}
                  className="min-h-0 flex-1"
                  selectedPath={selectedPath}
                  onSelectFile={setSelectedPath}
                />
              )}
            </Card>

            <Card className="flex min-h-0 flex-col rounded-[12px] p-0 overflow-hidden">
              <SpecFileContentPreview specId={spec.id} path={selectedPath} />
            </Card>
          </div>
        </div>

        <div className="flex shrink-0 justify-end mt-6">
          {canEdit && (
            <Link to={`/studio/${spec.id}`} className={cn(buttonVariants())}>
              {spec.status === "building"
                ? "View generation progress"
                : spec.status === "failed"
                  ? "Retry generation"
                  : "Continue in Studio"}
            </Link>
          )}
          {generationReady && !latestGeneration && (
            <Link
              to={`/specifications/${spec.id}/launch`}
              className={cn(buttonVariants())}
            >
              Start AI Build
            </Link>
          )}
          {generationReady && latestGeneration?.status === "ready" && (
            <Link
              to={`/specifications/${spec.id}/launch`}
              className={cn(buttonVariants())}
            >
              Rebuild
            </Link>
          )}
          {generationReady && latestGeneration?.status === "failed" && (
            <Link
              to={`/specifications/${spec.id}/launch`}
              className={cn(buttonVariants())}
            >
              Retry AI Build
            </Link>
          )}
          {generationReady && latestGeneration?.status === "running" && (
            <Link
              to={`/build/${spec.id}?generation=${latestGeneration.id}`}
              className={cn(buttonVariants())}
            >
              View build progress
            </Link>
          )}
        </div>
      </div>

      <AlertDialog
        open={confirmingDelete}
        onOpenChange={(open) => {
          if (!open && !deleteSpecification.isPending) setConfirmingDelete(false);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogMedia>
              <CircleAlertIcon />
            </AlertDialogMedia>
            <AlertDialogTitle>Delete specification?</AlertDialogTitle>
            <AlertDialogDescription>
              This permanently removes {spec.name} and all of its phases. This
              can't be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleteSpecification.isPending}>
              Cancel
            </AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              disabled={deleteSpecification.isPending}
              onClick={confirmDelete}
            >
              {deleteSpecification.isPending ? "Deleting…" : "Delete specification"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </main>
  );
}

function MetaTile({ label, value }: { label: string; value: string }) {
  return (
    <Card className="gap-0 rounded-lg p-3.5">
      <div className="text-[11px] text-muted-foreground">{label}</div>
      <div className="text-sm font-semibold mt-0.5">{value}</div>
    </Card>
  );
}

function Section({
  title,
  children,
}: {
  title: ReactNode;
  children: ReactNode;
}) {
  return (
    <div>
      <div className="text-[13px] font-semibold mb-2">{title}</div>
      {children}
    </div>
  );
}
