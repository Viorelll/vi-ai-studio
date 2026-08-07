import { useState } from "react";
import { Link } from "react-router-dom";
import { ChevronRightIcon } from "lucide-react";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";
import { StatusBadge } from "@/components/status-badge";
import { formatDateTime } from "@/lib/format";
import { cn } from "@/lib/utils";
import type { GenerationStatus, GenerationSummary, SpecificationSummary } from "@/lib/types";

const DOT_COLOR: Record<GenerationStatus, string> = {
  running: "bg-blue-700",
  ready: "bg-green-700",
  failed: "bg-red-700",
};

export function GeneratedProjectCard({
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
    <Card className="p-0 overflow-hidden">
      <button
        type="button"
        onClick={() => setExpanded((v) => !v)}
        className="w-full flex items-center justify-between px-5 py-4.5 hover:bg-muted/50 text-left"
      >
        <div className="flex items-center gap-3 min-w-0">
          <ChevronRightIcon className={`size-3.5 text-muted-foreground shrink-0 transition-transform ${expanded ? "rotate-90" : ""}`} />
          <div className="min-w-0">
            <div className="text-[14.5px] font-bold truncate">{spec.name}</div>
            <div className="text-xs text-muted-foreground mt-0.5 truncate">
              {spec.summary} · {spec.owner}
            </div>
          </div>
        </div>
        <div className="flex items-center gap-3.5 shrink-0">
          {latest && (
            <div className="text-right">
              <div className="text-[12.5px] font-semibold">v{latest.version}</div>
              <div className="text-[11.5px] text-muted-foreground mt-0.5">
                Latest: {formatDateTime(latest.created)}
              </div>
            </div>
          )}
          <StatusBadge status={spec.status} />
        </div>
      </button>

      {expanded && (
        <div className="border-t px-5 pt-1.5 pb-4">
          <div className="relative pl-5">
            <div className="absolute left-[5px] top-1.5 bottom-1.5 w-px bg-border" />
            {sorted.map((gen) => (
              <Link
                key={gen.id}
                to={`/generated/${spec.id}/versions/${gen.version}`}
                className="relative block py-3.5 border-b last:border-b-0 hover:bg-muted/40 -mx-5 px-5"
              >
                <span
                  className={cn(
                    "absolute left-[-15px] top-[19px] size-2.5 rounded-full ring-2 ring-background",
                    DOT_COLOR[gen.status],
                  )}
                />
                <div className="flex items-center justify-between gap-3">
                  <div className="flex items-center gap-2.5 min-w-0">
                    <span className="text-[13px] font-bold font-mono">v{gen.version}</span>
                    {gen.version === latest?.version && (
                      <Badge variant="secondary" className="text-[10px] uppercase tracking-wide">
                        Latest
                      </Badge>
                    )}
                    <span className="text-[12.5px] text-foreground/80 truncate">{gen.note}</span>
                  </div>
                  <StatusBadge status={gen.status} />
                </div>
                <div className="flex items-center gap-3.5 mt-1.5 text-xs text-muted-foreground">
                  <span>{formatDateTime(gen.created)}</span>
                  {gen.durationSeconds != null && (
                    <>
                      <span>·</span>
                      <span>Build time {gen.durationSeconds}s</span>
                    </>
                  )}
                </div>
                <div className="flex gap-1.5 flex-wrap mt-2">
                  {[gen.stack.backend, gen.stack.ui, gen.stack.database, gen.stack.infra].map((tech) => (
                    <Badge key={tech} variant="secondary" className="font-normal">
                      {tech}
                    </Badge>
                  ))}
                </div>
              </Link>
            ))}
          </div>
          <div className="flex justify-end mt-2">
            <Link
              to={`/specifications/${spec.id}`}
              className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
            >
              Open specification
            </Link>
          </div>
        </div>
      )}
    </Card>
  );
}
