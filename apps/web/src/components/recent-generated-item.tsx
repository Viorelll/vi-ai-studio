import { useState } from "react";
import { Link } from "react-router-dom";
import { ChevronRightIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import { formatDateTime } from "@/lib/format";
import { getStatusBadgeClassName, getStatusLabel } from "@/lib/status";
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
    <Collapsible
      open={expanded}
      onOpenChange={setExpanded}
      className="border-b border-[#f0f0f1] last:border-b-0"
    >
      <CollapsibleTrigger
        render={
          <Button
            type="button"
            variant="ghost"
            className="h-auto min-h-19 w-full flex-col items-stretch gap-1.5 rounded-none px-4.5 py-3.5 text-left hover:bg-muted/50"
          >
            <div className="flex items-center justify-between gap-2.5 min-w-0">
              <div className="flex items-center gap-2 min-w-0">
                <ChevronRightIcon className="size-3 text-muted-foreground shrink-0 transition-transform group-aria-expanded/button:rotate-90" />
                <span className="text-[13px] font-semibold truncate">
                  {spec.name}
                </span>
              </div>
              <div className="shrink-0">
                <Badge
                  variant="outline"
                  className={getStatusBadgeClassName(spec.status)}
                >
                  {getStatusLabel(spec.status)}
                </Badge>
              </div>
            </div>
            <div className="flex min-w-0 items-center gap-2 pl-4.5">
              <Badge
                variant="secondary"
                className="h-auto rounded-full border px-2 py-px text-[11px] font-semibold text-muted-foreground"
              >
                {generations.length === 1
                  ? "1 version"
                  : `${generations.length} versions`}
              </Badge>
              {latest && (
                <span className="min-w-0 truncate text-xs text-muted-foreground">
                  Latest: {formatDateTime(latest.created)}
                </span>
              )}
            </div>
          </Button>
        }
      />

      <CollapsibleContent>
        <div className="pl-10 pr-4.5 pb-3 flex flex-col gap-2">
          {sorted.map((gen, i) => (
            <Link
              key={gen.version}
              to={`/generated/${spec.id}/versions/${gen.version}`}
              className="flex items-center gap-2 hover:opacity-70"
            >
              <span className="text-[12.5px] font-bold font-mono">
                v{gen.version}
              </span>
              {i === 0 && (
                <Badge
                  variant="secondary"
                  className="h-auto rounded-full px-1.5 py-0.5 text-[10px] font-bold uppercase"
                >
                  Latest
                </Badge>
              )}
              <span className="text-xs text-muted-foreground">
                {gen.model} · {formatDateTime(gen.created)}
              </span>
            </Link>
          ))}
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
}
