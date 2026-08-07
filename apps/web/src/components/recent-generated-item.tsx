import { useState } from "react";
import { Link } from "react-router-dom";
import { ChevronRightIcon } from "lucide-react";
import { StatusBadge } from "@/components/status-badge";
import { formatDateTime } from "@/lib/format";
import type { GenerationSummary, SpecificationSummary } from "@/lib/types";

export function RecentGeneratedItem({
  spec,
  generations,
}: {
  spec: SpecificationSummary;
  generations: GenerationSummary[];
}) {
  const [expanded, setExpanded] = useState(false);
  const sorted = [...generations].sort((a, b) => b.version - a.version);
  const latest = sorted[0];

  return (
    <div className="border-b last:border-b-0">
      <button
        type="button"
        onClick={() => setExpanded((v) => !v)}
        className="w-full flex flex-col gap-1.5 px-4.5 py-3.5 text-left hover:bg-muted/50"
      >
        <div className="flex items-center justify-between gap-2.5">
          <div className="flex items-center gap-2 min-w-0">
            <ChevronRightIcon
              className={`size-3 text-muted-foreground shrink-0 transition-transform ${expanded ? "rotate-90" : ""}`}
            />
            <span className="text-[13px] font-semibold truncate">{spec.name}</span>
          </div>
          <StatusBadge status={spec.status} />
        </div>
        <div className="flex items-center gap-2 pl-[18px]">
          <span className="text-[11px] font-semibold text-muted-foreground bg-muted border rounded-full px-2 py-px">
            {generations.length === 1 ? "1 version" : `${generations.length} versions`}
          </span>
          {latest && <span className="text-xs text-muted-foreground">Latest: {formatDateTime(latest.created)}</span>}
        </div>
      </button>

      {expanded && (
        <div className="pl-10 pr-4.5 pb-3 flex flex-col gap-2">
          {sorted.map((gen, i) => (
            <Link
              key={gen.version}
              to={`/generated/${spec.id}/versions/${gen.version}`}
              className="flex items-center gap-2 hover:opacity-70"
            >
              <span className="text-[12.5px] font-bold font-mono">v{gen.version}</span>
              {i === 0 && (
                <span className="text-[10px] font-bold uppercase bg-muted px-1.5 py-0.5 rounded-full">Latest</span>
              )}
              <span className="text-xs text-muted-foreground">
                {gen.model} · {formatDateTime(gen.created)}
              </span>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
