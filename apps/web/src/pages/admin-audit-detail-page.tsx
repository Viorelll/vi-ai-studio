import { useParams, useSearchParams } from "react-router-dom";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { AuditLogTable } from "@/components/audit-log-table";
import { PageLoading } from "@/components/page-loading";
import { NotFoundView } from "@/components/not-found-view";
import { useSpecifications } from "@/hooks/use-specifications";
import { useAuditLogs } from "@/hooks/use-audit";

export function AdminAuditDetailPage() {
  const { mode, id } = useParams<{ mode: string; id: string }>();
  const [searchParams] = useSearchParams();
  const versionFilter = searchParams.get("version");

  const specsQuery = useSpecifications();
  const logsQuery = useAuditLogs(id);

  if (specsQuery.isPending || logsQuery.isPending) return <PageLoading />;

  const spec = specsQuery.data?.find((s) => s.id === id);
  if (!spec) return <NotFoundView message="This specification doesn't exist." />;

  const allLogs = logsQuery.data ?? [];
  const logs = versionFilter ? allLogs.filter((l) => l.generationVersion === Number(versionFilter)) : allLogs;

  const totalRequests = logs.reduce((sum, l) => sum + l.requests, 0);
  const totalTokensIn = logs.reduce((sum, l) => sum + l.tokensIn, 0);
  const totalTokensOut = logs.reduce((sum, l) => sum + l.tokensOut, 0);

  const modeTitle = mode === "specifications" ? "Project specifications" : "Generated projects";
  const modeHref = mode === "specifications" || mode === "generated" ? `/admin/audit/${mode}` : "/admin/audit";

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[1000px]">
        <PageBreadcrumb
          items={[
            { label: "Admin", href: "/admin" },
            { label: "Audit", href: "/admin/audit" },
            { label: modeTitle, href: modeHref },
            { label: spec.name },
          ]}
        />

        <div className="text-[17px] font-bold">
          {spec.name}
          {versionFilter && <span className="text-muted-foreground font-mono"> · v{versionFilter}</span>}
        </div>
        <p className="text-[13px] text-muted-foreground mb-4.5">{spec.summary}</p>

        <div className="grid grid-cols-3 gap-3 mb-5.5">
          <MetricTile label="Total requests" value={totalRequests.toLocaleString("en-US")} />
          <MetricTile label="Tokens in" value={totalTokensIn.toLocaleString("en-US")} />
          <MetricTile label="Tokens out" value={totalTokensOut.toLocaleString("en-US")} />
        </div>

        <AuditLogTable logs={logs} />
      </div>
    </main>
  );
}

function MetricTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border bg-card p-4">
      <div className="text-[22px] font-bold">{value}</div>
      <div className="text-xs text-muted-foreground mt-0.5">{label}</div>
    </div>
  );
}
