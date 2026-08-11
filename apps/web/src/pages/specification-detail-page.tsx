import { type ReactNode, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { CircleAlertIcon, FolderIcon, Trash2Icon } from "lucide-react";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { DownloadButton } from "@/components/download-button";
import { FileTree } from "@/components/file-tree";
import { PageLoading } from "@/components/page-loading";
import { NotFoundView } from "@/components/not-found-view";
import { Badge } from "@/components/ui/badge";
import { Card, CardHeader } from "@/components/ui/card";
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
import { useDeleteSpecification, useSpecification } from "@/hooks/use-specifications";
import { SPEC_DOC_PATHS } from "@/lib/spec-doc-paths";
import { ApiError } from "@/lib/api-client";
import { formatDate } from "@/lib/format";
import { getStatusBadgeClassName, getStatusLabel } from "@/lib/status";
import { cn } from "@/lib/utils";

export function SpecificationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const specQuery = useSpecification(id);
  const deleteSpecification = useDeleteSpecification();
  const [confirmingDelete, setConfirmingDelete] = useState(false);

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
  const canEdit = spec.status === "draft";
  const latestGeneration = spec.generations[0]; // API orders these newest-first

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="flex h-[calc(100vh-3.5rem-5rem)] w-full max-w-[820px] flex-col">
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
          <Section title="Description">
            <p className="text-sm text-foreground/90 leading-relaxed whitespace-pre-wrap">
              {spec.description || "No description yet."}
            </p>
          </Section>

          <Section title="Requirements & features">
            <p className="text-sm text-foreground/90 leading-relaxed whitespace-pre-wrap">
              {spec.features || "No requirements captured yet."}
            </p>
          </Section>

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

        <Card className="flex min-h-0 flex-1 flex-col rounded-[12px] p-0 overflow-hidden">
          <CardHeader className="flex items-center justify-between gap-2 rounded-none border-b bg-[#fafafa] px-5 py-3 text-xs font-semibold text-muted-foreground">
            <span className="flex items-center gap-2">
              <FolderIcon className="size-3.5" />
              Specifications (.md)
              <span className="font-normal">
                · {SPEC_DOC_PATHS.length} files
              </span>
            </span>
            <DownloadButton
              path={`/api/specifications/${spec.id}/download`}
              filename={`${spec.name}-specification.zip`}
              variant="outline"
              size="sm"
            />
          </CardHeader>
          <FileTree
            paths={SPEC_DOC_PATHS}
            bordered={false}
            className="min-h-0 flex-1"
          />
        </Card>

        <div className="flex shrink-0 justify-end mt-6">
          {canEdit && (
            <Link to={`/studio/${spec.id}`} className={cn(buttonVariants())}>
              Continue in Studio
            </Link>
          )}
          {!canEdit && !latestGeneration && (
            <Link
              to={`/specifications/${spec.id}/launch`}
              className={cn(buttonVariants())}
            >
              Start AI Build
            </Link>
          )}
          {!canEdit && latestGeneration?.status === "ready" && (
            <Link
              to={`/specifications/${spec.id}/launch`}
              className={cn(buttonVariants())}
            >
              Rebuild
            </Link>
          )}
          {!canEdit && latestGeneration?.status === "failed" && (
            <Link
              to={`/specifications/${spec.id}/launch`}
              className={cn(buttonVariants())}
            >
              Retry AI Build
            </Link>
          )}
          {!canEdit && latestGeneration?.status === "running" && (
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
