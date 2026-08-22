import { Link, useParams } from "react-router-dom";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { PageLoading } from "@/components/page-loading";
import { NotFoundView } from "@/components/not-found-view";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";
import { TASK_LABELS } from "@/components/audit-log-table";
import { useSpecifications } from "@/hooks/use-specifications";
import { useAuditLog } from "@/hooks/use-audit";
import { formatDateTime } from "@/lib/format";
import { cn } from "@/lib/utils";

export function AdminAuditLogDetailPage() {
  const { mode, id, logId } = useParams<{
    mode: string;
    id: string;
    logId: string;
  }>();

  const specsQuery = useSpecifications();
  const logQuery = useAuditLog(logId);

  if (specsQuery.isPending || logQuery.isPending) return <PageLoading />;

  const spec = specsQuery.data?.find((s) => s.id === id);
  if (!spec) return <NotFoundView message="This specification doesn't exist." />;

  const log = logQuery.data;
  if (!log) return <NotFoundView message="This log entry doesn't exist." />;

  const modeTitle =
    mode === "specifications" ? "Project specifications" : "Generated projects";
  const specHref =
    mode === "specifications" || mode === "generated"
      ? `/admin/audit/${mode}/${id}`
      : "/admin/audit";

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[900px]">
        <PageBreadcrumb
          items={[
            { label: "Admin", href: "/admin" },
            { label: "Audit", href: "/admin/audit" },
            { label: modeTitle, href: `/admin/audit/${mode}` },
            { label: spec.name, href: specHref },
            { label: "Log entry" },
          ]}
        />

        <div className="flex items-start justify-between gap-4 mb-5.5">
          <div>
            <h1 className="text-[20px] font-bold tracking-tight flex items-center gap-2.5">
              {log.model}
              <Badge variant="secondary" className="h-auto rounded-full px-2 py-0.5 text-[11px] font-semibold">
                {TASK_LABELS[log.task] ?? log.task}
              </Badge>
            </h1>
            <p className="text-[13px] text-muted-foreground mt-1">
              {formatDateTime(log.created)}
            </p>
          </div>
          <Link to={specHref} className={cn(buttonVariants({ variant: "outline" }))}>
            ← Back to {spec.name}
          </Link>
        </div>

        <div className="grid grid-cols-3 gap-3 mb-5.5">
          <MetricTile label="Requests" value={log.requests.toLocaleString("en-US")} />
          <MetricTile label="Tokens in" value={log.tokensIn.toLocaleString("en-US")} />
          <MetricTile label="Tokens out" value={log.tokensOut.toLocaleString("en-US")} />
        </div>

        <div className="flex flex-col gap-5">
          <div>
            <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-1.5">
              Input prompt
            </div>
            <Card className="gap-0 rounded-[10px] bg-[#fafafa] p-4.5 text-[13.5px] leading-relaxed whitespace-pre-wrap">
              {log.prompt}
            </Card>
          </div>
          <div>
            <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-1.5">
              Result
            </div>
            <Card className="gap-0 rounded-[10px] bg-[#fafafa] p-4.5 text-[13.5px] leading-relaxed whitespace-pre-wrap">
              {log.result}
            </Card>
          </div>
        </div>
      </div>
    </main>
  );
}

function MetricTile({ label, value }: { label: string; value: string }) {
  return (
    <Card className="gap-0 rounded-lg p-4">
      <div className="text-[22px] font-bold">{value}</div>
      <div className="text-xs text-muted-foreground mt-0.5">{label}</div>
    </Card>
  );
}
