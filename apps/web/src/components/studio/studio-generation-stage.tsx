import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { CheckIcon, FileIcon, FolderIcon, XIcon } from "lucide-react";
import { Card, CardHeader } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Spinner } from "@/components/ui/spinner";
import {
  Timeline,
  TimelineConnector,
  TimelineContent,
  TimelineIndicator,
  TimelineItem,
  TimelineRail,
} from "@/components/ui/timeline";
import { FileTree } from "@/components/file-tree";
import {
  useSpecificationDocument,
  useSpecificationDocuments,
} from "@/hooks/use-specifications";
import {
  useGenerationRuns,
  useStartGenerationRun,
  useSpecificationGenerationStream,
  useValidationIssues,
} from "@/hooks/use-specification-generation";
import { useQueryClient } from "@tanstack/react-query";
import { ApiError } from "@/lib/api-client";
import { cn } from "@/lib/utils";
import type { SpecificationGenerationBatch } from "@/lib/types";

const BATCH_LABELS: Record<number, string> = {
  1: "Meta & rules",
  2: "Product",
  3: "Architecture",
  4: "Database",
  5: "Backend",
  6: "Frontend",
  7: "Other deployables",
  8: "Infrastructure",
  9: "Quality",
  10: "Delivery",
};

function BatchTimeline({
  batches,
}: {
  batches: SpecificationGenerationBatch[];
}) {
  const sorted = [...batches].sort((a, b) => a.batchIndex - b.batchIndex);
  return (
    <Timeline>
      {sorted.map((batch, index) => {
        const isDone = batch.status === "ready";
        const isSkipped = batch.status === "skipped";
        const isFailed = batch.status === "failed";
        const isActive = batch.status === "running";
        return (
          <TimelineItem
            key={batch.batchIndex}
            className="grid-cols-[24px_minmax(0,1fr)] pb-5 last:pb-0"
          >
            <TimelineRail className="w-6">
              <TimelineIndicator
                className={cn(
                  "mt-0 flex size-6 items-center justify-center border-2 text-xs font-bold",
                  isDone
                    ? "border-[var(--brand)] bg-[var(--brand)] text-white"
                    : isFailed
                      ? "border-destructive bg-destructive text-white"
                      : isActive
                        ? "border-blue-200 bg-blue-50 text-blue-700"
                        : isSkipped
                          ? "border-border bg-muted text-muted-foreground"
                          : "border-border bg-card text-muted-foreground",
                )}
              >
                {isDone ? (
                  <CheckIcon className="size-3.5" />
                ) : isFailed ? (
                  <XIcon className="size-3.5" />
                ) : isActive ? (
                  <span className="size-2 rounded-full bg-blue-600" />
                ) : isSkipped ? (
                  <span className="size-1.5 rounded-full bg-muted-foreground" />
                ) : (
                  batch.batchIndex
                )}
              </TimelineIndicator>
              {index < sorted.length - 1 && (
                <TimelineConnector
                  className={cn(
                    "-bottom-5",
                    isDone ? "bg-[var(--brand)]" : "bg-border",
                  )}
                />
              )}
            </TimelineRail>
            <TimelineContent>
              <div
                className={cn(
                  "text-[13px] font-semibold",
                  isSkipped
                    ? "text-muted-foreground line-through"
                    : isDone || isActive || isFailed
                      ? "text-foreground"
                      : "text-muted-foreground",
                )}
              >
                {BATCH_LABELS[batch.batchIndex] ?? batch.name}
              </div>
              <div className="text-xs text-muted-foreground mt-0.5">
                {isDone
                  ? `${batch.filesWritten} file${batch.filesWritten === 1 ? "" : "s"}`
                  : isSkipped
                    ? batch.note
                    : isFailed
                      ? batch.note
                      : isActive
                        ? "Generating…"
                        : "Waiting"}
              </div>
            </TimelineContent>
          </TimelineItem>
        );
      })}
    </Timeline>
  );
}

function GenerationFilePreview({
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

export function StudioGenerationStage({ specId }: { specId: string }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const runsQuery = useGenerationRuns(specId);
  const startRun = useStartGenerationRun(specId);
  const documentsQuery = useSpecificationDocuments(specId);
  const validationIssuesQuery = useValidationIssues(specId);

  const [activeRunId, setActiveRunId] = useState<string | null>(null);
  const [selectedPath, setSelectedPath] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const latestRun = runsQuery.data?.[0] ?? null;
  const runId = activeRunId ?? latestRun?.id ?? null;
  const isWatching = latestRun
    ? latestRun.status === "pending" || latestRun.status === "running"
    : Boolean(activeRunId);

  const { done } = useSpecificationGenerationStream(
    specId,
    isWatching ? runId : null,
    () => {
      queryClient.invalidateQueries({
        queryKey: ["specifications", specId, "generation-runs"],
      });
      queryClient.invalidateQueries({
        queryKey: ["specifications", specId, "documents"],
      });
    },
  );

  useEffect(() => {
    if (done) {
      queryClient.invalidateQueries({
        queryKey: ["specifications", specId, "validation-issues"],
      });
    }
  }, [done, specId, queryClient]);

  useEffect(() => {
    if (
      documentsQuery.data &&
      documentsQuery.data.length > 0 &&
      !selectedPath
    ) {
      setSelectedPath(documentsQuery.data[0]);
    }
  }, [documentsQuery.data, selectedPath]);

  async function handleStart() {
    setError(null);
    try {
      const run = await startRun.mutateAsync();
      setActiveRunId(run.id);
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Couldn't start generation.",
      );
    }
  }

  if (runsQuery.isPending) {
    return (
      <div className="flex items-center gap-2 p-10 text-sm text-muted-foreground">
        <Spinner />
        Loading…
      </div>
    );
  }

  const hasRun = Boolean(latestRun);
  const runReady = latestRun?.status === "ready";
  const runFailed = latestRun?.status === "failed";

  return (
    <div className="flex flex-col gap-5">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-xl font-bold tracking-tight mb-1.5">
            Generate the specification
          </h2>
          <p className="text-sm text-muted-foreground max-w-lg">
            Ten batches turn your selections and interview into the full
            specification tree -- product, architecture, database, backend,
            infrastructure, quality and delivery.
          </p>
        </div>
        {(!hasRun || runFailed) && (
          <Button
            onClick={handleStart}
            disabled={startRun.isPending || isWatching}
          >
            {startRun.isPending
              ? "Starting…"
              : runFailed
                ? "Retry generation"
                : "Start generation"}
          </Button>
        )}
        {runReady && (
          <Button onClick={() => navigate(`/specifications/${specId}`)}>
            View specification →
          </Button>
        )}
      </div>

      {error && <p className="text-[12.5px] text-destructive">{error}</p>}

      {latestRun && (
        <>
          <div className="grid grid-cols-1 lg:grid-cols-[240px_1fr] gap-6">
            <Card className="rounded-[12px] p-5">
              <BatchTimeline batches={latestRun.batches} />
            </Card>

            <div className="flex flex-col gap-3">
              <div className="flex items-center justify-between">
                <span className="flex items-center gap-2 text-xs font-semibold text-muted-foreground">
                  <FolderIcon className="size-3.5" />
                  Specification files
                  <span className="font-normal">
                    ·{" "}
                    {documentsQuery.data
                      ? `${documentsQuery.data.length} files`
                      : "…"}
                  </span>
                </span>
              </div>
              <div className="grid h-145 grid-cols-[240px_1fr] gap-4">
                <Card className="flex min-h-0 flex-col rounded-[12px] p-0 overflow-hidden">
                  <FileTree
                    paths={documentsQuery.data ?? []}
                    bordered={false}
                    className="min-h-0 flex-1"
                    selectedPath={selectedPath}
                    onSelectFile={setSelectedPath}
                  />
                </Card>
                <Card className="flex min-h-0 flex-col rounded-[12px] p-0 overflow-hidden">
                  <GenerationFilePreview specId={specId} path={selectedPath} />
                </Card>
              </div>
            </div>
          </div>

          {validationIssuesQuery.data &&
            validationIssuesQuery.data.length > 0 && (
              <Card className="rounded-[12px] p-0 gap-0 overflow-hidden">
                <CardHeader className="rounded-none border-b bg-[#fefbeb] px-4 py-2.5 text-[12.5px] font-semibold text-amber-800">
                  {validationIssuesQuery.data.length} validation note
                  {validationIssuesQuery.data.length === 1 ? "" : "s"}
                </CardHeader>
                {/* Exactly 5 rows visible (5 * h-9), scroll for the rest. */}
                <ScrollArea className="max-h-45">
                  <ul className="flex flex-col">
                    {validationIssuesQuery.data.map((issue, i) => (
                      <li
                        key={i}
                        className="flex h-9 items-center gap-1.5 border-b px-4 text-[11.5px] text-amber-800 last:border-b-0"
                      >
                        <span className="font-mono shrink-0">{issue.code}</span>
                        <span className="min-w-0 truncate">
                          -- {issue.message}
                        </span>
                      </li>
                    ))}
                  </ul>
                </ScrollArea>
              </Card>
            )}
        </>
      )}
    </div>
  );
}
