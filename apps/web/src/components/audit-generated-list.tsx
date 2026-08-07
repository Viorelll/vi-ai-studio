import { useState } from "react";
import { Link } from "react-router-dom";
import { ChevronRightIcon } from "lucide-react";
import { Card } from "@/components/ui/card";
import { apiClient } from "@/lib/api-client";
import { formatDateTime } from "@/lib/format";
import type { AiCallLogRollup, AiCallLogSummary, SpecificationDetail, SpecificationSummary } from "@/lib/types";

interface VersionRollup {
  version: number;
  date: string;
  logCount: number;
  requests: number;
  tokens: number;
  latest: boolean;
}

export function AuditGeneratedList({ specs, rollups }: { specs: SpecificationSummary[]; rollups: AiCallLogRollup[] }) {
  const rollupById = new Map(rollups.map((r) => [r.specificationId, r]));
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [versionsBySpec, setVersionsBySpec] = useState<Record<string, VersionRollup[]>>({});
  const [loading, setLoading] = useState(false);

  async function toggle(spec: SpecificationSummary) {
    if (expandedId === spec.id) {
      setExpandedId(null);
      return;
    }
    setExpandedId(spec.id);
    if (versionsBySpec[spec.id]) return;

    setLoading(true);
    try {
      const [logs, detail] = await Promise.all([
        apiClient.get<AiCallLogSummary[]>(`/api/admin/audit/specifications/${spec.id}`),
        apiClient.get<SpecificationDetail>(`/api/specifications/${spec.id}`),
      ]);
      const dateByVersion = new Map(detail.generations.map((g) => [g.version, g.created]));
      const byVersion = new Map<number, AiCallLogSummary[]>();
      for (const log of logs) {
        if (log.generationVersion == null) continue;
        const list = byVersion.get(log.generationVersion) ?? [];
        list.push(log);
        byVersion.set(log.generationVersion, list);
      }
      const maxVersion = Math.max(0, ...byVersion.keys());
      const rows: VersionRollup[] = [...byVersion.entries()]
        .map(([version, entries]) => ({
          version,
          date: dateByVersion.get(version) ?? "",
          logCount: entries.length,
          requests: entries.reduce((sum, e) => sum + e.requests, 0),
          tokens: entries.reduce((sum, e) => sum + e.tokensIn + e.tokensOut, 0),
          latest: version === maxVersion,
        }))
        .sort((a, b) => b.version - a.version);
      setVersionsBySpec((prev) => ({ ...prev, [spec.id]: rows }));
    } finally {
      setLoading(false);
    }
  }

  if (specs.length === 0) {
    return <div className="text-center text-muted-foreground py-16 border rounded-xl bg-card">No generated projects yet.</div>;
  }

  return (
    <div className="flex flex-col gap-3">
      {specs.map((spec) => {
        const rollup = rollupById.get(spec.id);
        const expanded = expandedId === spec.id;
        const versions = versionsBySpec[spec.id];
        return (
          <Card key={spec.id} className="p-0 overflow-hidden">
            <button
              type="button"
              onClick={() => toggle(spec)}
              className="w-full flex items-center justify-between px-5 py-4 hover:bg-muted/50 text-left"
            >
              <div className="flex items-center gap-3 min-w-0">
                <ChevronRightIcon className={`size-3.5 text-muted-foreground shrink-0 transition-transform ${expanded ? "rotate-90" : ""}`} />
                <div className="min-w-0">
                  <div className="text-[13.5px] font-semibold truncate">{spec.name}</div>
                  <div className="text-xs text-muted-foreground mt-0.5 truncate">{spec.summary}</div>
                </div>
              </div>
              <div className="flex items-center gap-4.5 shrink-0 text-[12.5px] text-foreground/80">
                <span>{spec.generationCount} versions</span>
                <span>{rollup?.logCount ?? 0} logs</span>
                <span>{rollup?.totalRequests ?? 0} requests</span>
                <span className="font-mono">{(rollup?.totalTokens ?? 0).toLocaleString("en-US")} tok</span>
              </div>
            </button>

            {expanded && (
              <div className="border-t">
                <div className="grid grid-cols-[1fr_1.4fr_1fr_1fr_1fr] px-5 py-2.5 text-[11.5px] font-semibold text-muted-foreground bg-muted/40">
                  <div>Version</div>
                  <div>Generated</div>
                  <div>Log entries</div>
                  <div>Requests</div>
                  <div>Tokens</div>
                </div>
                {loading && !versions && <div className="px-5 py-4 text-sm text-muted-foreground">Loading…</div>}
                {versions?.map((v) => (
                  <Link
                    key={v.version}
                    to={`/admin/audit/specifications/${spec.id}?version=${v.version}`}
                    className="grid grid-cols-[1fr_1.4fr_1fr_1fr_1fr] items-center px-5 py-3 border-t hover:bg-muted/40"
                  >
                    <div className="flex items-center gap-2">
                      <span className="text-[13px] font-bold font-mono">v{v.version}</span>
                      {v.latest && <span className="text-[10px] font-bold uppercase bg-muted px-1.5 py-0.5 rounded-full">Latest</span>}
                    </div>
                    <div className="text-[12.5px] text-muted-foreground">{v.date ? formatDateTime(v.date) : ""}</div>
                    <div className="text-sm">{v.logCount}</div>
                    <div className="text-sm">{v.requests}</div>
                    <div className="text-sm font-mono">{v.tokens.toLocaleString("en-US")}</div>
                  </Link>
                ))}
              </div>
            )}
          </Card>
        );
      })}
    </div>
  );
}
