import { useState } from "react";
import { Link } from "react-router-dom";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Empty, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import { apiClient } from "@/lib/api-client";
import { formatDateTime } from "@/lib/format";
import type {
  AiCallLogRollup,
  AiCallLogSummary,
  SpecificationDetail,
  SpecificationSummary,
} from "@/lib/types";

interface VersionRollup {
  version: number;
  date: string;
  logCount: number;
  requests: number;
  tokens: number;
  latest: boolean;
}

export function AuditGeneratedList({
  specs,
  rollups,
}: {
  specs: SpecificationSummary[];
  rollups: AiCallLogRollup[];
}) {
  const rollupById = new Map(rollups.map((r) => [r.specificationId, r]));
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [versionsBySpec, setVersionsBySpec] = useState<
    Record<string, VersionRollup[]>
  >({});
  const [loading, setLoading] = useState(false);

  async function expand(spec: SpecificationSummary) {
    setExpandedId(spec.id);
    if (versionsBySpec[spec.id]) return;

    setLoading(true);
    try {
      const [logs, detail] = await Promise.all([
        apiClient.get<AiCallLogSummary[]>(
          `/api/admin/audit/specifications/${spec.id}?scope=build`,
        ),
        apiClient.get<SpecificationDetail>(`/api/specifications/${spec.id}`),
      ]);
      const dateByVersion = new Map(
        detail.generations.map((g) => [g.version, g.created]),
      );
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

  function handleExpandedChange(value: string[]) {
    const expandedSpecificationId = value[0] ?? null;
    if (!expandedSpecificationId) {
      setExpandedId(null);
      return;
    }

    const spec = specs.find((item) => item.id === expandedSpecificationId);
    if (spec) void expand(spec);
  }

  if (specs.length === 0) {
    return (
      <Empty className="rounded-[12px] border-[#e4e4e7] bg-card py-16">
        <EmptyHeader>
          <EmptyTitle>No generated projects yet.</EmptyTitle>
        </EmptyHeader>
      </Empty>
    );
  }

  return (
    <Accordion
      value={expandedId ? [expandedId] : []}
      onValueChange={handleExpandedChange}
      className="gap-3"
    >
      {specs.map((spec) => {
        const rollup = rollupById.get(spec.id);
        const versions = versionsBySpec[spec.id];
        return (
          <Card key={spec.id} className="gap-0 overflow-hidden p-0">
            <AccordionItem value={spec.id} className="border-0">
              <AccordionTrigger className="h-auto rounded-none px-5 py-4 text-left hover:no-underline hover:bg-muted/50">
                <div className="min-w-0">
                  <div className="text-[13.5px] font-semibold truncate">
                    {spec.name}
                  </div>
                  <div className="mt-0.5 truncate text-xs text-muted-foreground">
                    {spec.summary}
                  </div>
                </div>
                <div className="flex shrink-0 items-center gap-4.5 text-[12.5px] text-foreground/80">
                  <span>{spec.generationCount} versions</span>
                  <span>{rollup?.logCount ?? 0} logs</span>
                  <span>{rollup?.totalRequests ?? 0} requests</span>
                  <span className="font-mono">
                    {(rollup?.totalTokens ?? 0).toLocaleString("en-US")} tok
                  </span>
                </div>
              </AccordionTrigger>

              <AccordionContent className="p-0">
                <div className="border-t">
                  <div className="grid grid-cols-[1fr_1.4fr_1fr_1fr_1fr] bg-muted/40 px-5 py-2.5 text-[11.5px] font-semibold text-muted-foreground">
                    <div>Version</div>
                    <div>Generated</div>
                    <div>Log entries</div>
                    <div>Requests</div>
                    <div>Tokens</div>
                  </div>
                  {loading && !versions && (
                    <div className="px-5 py-4 text-sm text-muted-foreground">
                      Loading…
                    </div>
                  )}
                  {versions?.map((v) => (
                    <Link
                      key={v.version}
                      to={`/admin/audit/generated/${spec.id}?version=${v.version}`}
                      className="grid grid-cols-[1fr_1.4fr_1fr_1fr_1fr] items-center border-t px-5 py-3 hover:bg-muted/40"
                    >
                      <div className="flex items-center gap-2">
                        <span className="text-[13px] font-bold font-mono">
                          v{v.version}
                        </span>
                        {v.latest && (
                          <Badge
                            variant="secondary"
                            className="h-auto rounded-full px-1.5 py-0.5 text-[10px] font-bold uppercase"
                          >
                            Latest
                          </Badge>
                        )}
                      </div>
                      <div className="text-[12.5px] text-muted-foreground">
                        {v.date ? formatDateTime(v.date) : ""}
                      </div>
                      <div className="text-sm">{v.logCount}</div>
                      <div className="text-sm">{v.requests}</div>
                      <div className="text-sm font-mono">
                        {v.tokens.toLocaleString("en-US")}
                      </div>
                    </Link>
                  ))}
                </div>
              </AccordionContent>
            </AccordionItem>
          </Card>
        );
      })}
    </Accordion>
  );
}
