import { Link } from "react-router-dom";
import { Card } from "@/components/ui/card";
import { Empty, EmptyDescription } from "@/components/ui/empty";
import { formatDateTime } from "@/lib/format";
import type { AiCallLogSummary } from "@/lib/types";

export const TASK_LABELS: Record<string, string> = {
  specGeneration: "Spec generation",
  codeGeneration: "Code generation",
  imageGeneration: "Image generation",
  soundGeneration: "Sound generation",
  transcribing: "Transcribing",
};

const GRID_COLS = "grid-cols-[1.3fr_1fr_1fr_0.8fr_1fr]";

export function AuditLogTable({
  logs,
  basePath,
}: {
  logs: AiCallLogSummary[];
  /** Route prefix each row links to, as `${basePath}/logs/${log.id}`. */
  basePath: string;
}) {
  return (
    <Card className="gap-0 overflow-hidden p-0">
      <div
        className={`grid ${GRID_COLS} bg-muted/40 px-5 py-2.5 text-[11.5px] font-semibold text-muted-foreground`}
      >
        <div>Time</div>
        <div>Model</div>
        <div>Task</div>
        <div>Requests</div>
        <div>Tokens (in/out)</div>
      </div>

      {logs.length === 0 ? (
        <Empty className="min-h-0 border-0 py-10">
          <EmptyDescription>No log entries yet.</EmptyDescription>
        </Empty>
      ) : (
        logs.map((log) => (
          <Link
            key={log.id}
            to={`${basePath}/logs/${log.id}`}
            className={`grid ${GRID_COLS} items-center border-t px-5 py-3 hover:bg-muted/40`}
          >
            <span className="text-xs text-muted-foreground">
              {formatDateTime(log.created)}
            </span>
            <span className="text-sm font-semibold">{log.model}</span>
            <span className="text-sm">
              {TASK_LABELS[log.task] ?? log.task}
            </span>
            <span className="text-sm">{log.requests}</span>
            <span className="text-xs font-mono">
              {log.tokensIn.toLocaleString("en-US")} /{" "}
              {log.tokensOut.toLocaleString("en-US")}
            </span>
          </Link>
        ))
      )}
    </Card>
  );
}
