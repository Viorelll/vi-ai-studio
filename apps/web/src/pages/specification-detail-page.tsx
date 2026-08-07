import type { ReactNode } from "react";
import { Link, useParams } from "react-router-dom";
import { StatusBadge } from "@/components/status-badge";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { FileTree } from "@/components/file-tree";
import { PageLoading } from "@/components/page-loading";
import { NotFoundView } from "@/components/not-found-view";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { buttonVariants } from "@/components/ui/button";
import { useSpecification } from "@/hooks/use-specifications";
import { SPEC_DOC_PATHS } from "@/lib/spec-doc-paths";
import { API_BASE_URL, ApiError } from "@/lib/api-client";
import { cn } from "@/lib/utils";

export function SpecificationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const specQuery = useSpecification(id);

  if (specQuery.isPending) return <PageLoading />;
  if (specQuery.isError) {
    if (specQuery.error instanceof ApiError && specQuery.error.status === 404) {
      return <NotFoundView message="This specification doesn't exist." />;
    }
    throw specQuery.error;
  }
  const spec = specQuery.data;

  const stackChips = [spec.stack.backend, spec.stack.ui, spec.stack.database, spec.stack.infra];
  const canEdit = spec.status === "draft";
  const isFinalized = Boolean(spec.specMarkdown);

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[820px]">
        <PageBreadcrumb items={[{ label: "Project Specifications", href: "/specifications" }, { label: spec.name }]} />

        <div className="flex items-start justify-between gap-3 mb-1.5">
          <h1 className="text-[22px] font-bold tracking-tight">{spec.name}</h1>
          <StatusBadge status={spec.status} />
        </div>
        <p className="text-sm text-muted-foreground mb-7">{spec.summary || "No summary provided."}</p>

        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
          <MetaTile label="Owner" value={spec.owner} />
          <MetaTile label="Created" value={new Date(spec.created).toLocaleDateString("en-US")} />
          <MetaTile label="Progress" value={`${spec.progress}%`} />
          <MetaTile label="Audience" value={spec.audience || "Not specified"} />
        </div>

        <Card className="p-6 gap-5">
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

          <Section
            title={
              <span className="flex items-center justify-between">
                <span>
                  Specifications (.md) <span className="font-normal text-muted-foreground">· {SPEC_DOC_PATHS.length} files</span>
                </span>
                <a
                  href={`${API_BASE_URL}/api/specifications/${spec.id}/download`}
                  className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
                >
                  Download .zip
                </a>
              </span>
            }
          >
            <FileTree paths={SPEC_DOC_PATHS} />
          </Section>
        </Card>

        <div className="flex justify-end mt-6">
          {canEdit && !isFinalized && (
            <Link to={`/studio/${spec.id}`} className={cn(buttonVariants())}>
              Continue in Studio
            </Link>
          )}
          {canEdit && isFinalized && (
            <Link to={`/specifications/${spec.id}/launch`} className={cn(buttonVariants())}>
              Start AI Build
            </Link>
          )}
          {spec.status === "ready" && (
            <Link to={`/specifications/${spec.id}/launch`} className={cn(buttonVariants())}>
              Rebuild
            </Link>
          )}
          {spec.status === "failed" && (
            <Link to={`/specifications/${spec.id}/launch`} className={cn(buttonVariants())}>
              Retry AI Build
            </Link>
          )}
          {spec.status === "building" && (
            <Link
              to={`/build/${spec.id}?generation=${spec.generations.find((g) => g.status === "running")?.id ?? ""}`}
              className={cn(buttonVariants())}
            >
              View build progress
            </Link>
          )}
        </div>
      </div>
    </main>
  );
}

function MetaTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border bg-card p-3.5">
      <div className="text-[11px] text-muted-foreground">{label}</div>
      <div className="text-sm font-semibold mt-0.5">{value}</div>
    </div>
  );
}

function Section({ title, children }: { title: ReactNode; children: ReactNode }) {
  return (
    <div>
      <div className="text-[13px] font-semibold mb-2">{title}</div>
      {children}
    </div>
  );
}
