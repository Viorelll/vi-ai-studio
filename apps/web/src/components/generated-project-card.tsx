import { useState } from "react";
import { Link } from "react-router-dom";
import { ChevronRightIcon } from "lucide-react";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
  Timeline,
  TimelineConnector,
  TimelineContent,
  TimelineIndicator,
  TimelineItem,
  TimelineRail,
} from "@/components/ui/timeline";
import { formatDateTime, formatDuration } from "@/lib/format";
import { getStatusBadgeClassName, getStatusLabel } from "@/lib/status";
import { cn } from "@/lib/utils";
import type {
  GenerationStatus,
  GenerationSummary,
  SpecificationSummary,
} from "@/lib/types";

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
    <Card className="gap-0 overflow-hidden rounded-[12px] border-[#e4e4e7] bg-white p-0">
      <Collapsible open={expanded} onOpenChange={setExpanded}>
        <CollapsibleTrigger
          render={
            <Button
              type="button"
              variant="ghost"
              className="h-auto w-full justify-between rounded-none bg-transparent px-5 py-4.5 text-left hover:bg-[#fafafa]"
            >
              <div className="flex items-center gap-3 min-w-0">
                <ChevronRightIcon className="size-3 shrink-0 text-[#a1a1aa] transition-transform group-aria-expanded/button:rotate-90" />
                <div className="min-w-0">
                  <div className="text-[14.5px] font-bold truncate">
                    {spec.name}
                  </div>
                  <div className="mt-0.5 truncate text-[12px] text-[#a1a1aa]">
                    {spec.summary} · {spec.owner}
                  </div>
                </div>
              </div>
              <div className="flex items-center gap-3.5 shrink-0">
                {latest && (
                  <>
                    <div className="text-right">
                      <div className="text-[12.5px] font-semibold">
                        v{latest.version}
                      </div>
                      <div className="mt-0.5 text-[11.5px] text-[#a1a1aa]">
                        Latest: {formatDateTime(latest.created)}
                        {latest.durationSeconds != null && (
                          <> · {formatDuration(latest.durationSeconds)}</>
                        )}
                      </div>
                    </div>
                    <Badge
                      variant="outline"
                      className={getStatusBadgeClassName(latest.status)}
                    >
                      {getStatusLabel(latest.status)}
                    </Badge>
                  </>
                )}
              </div>
            </Button>
          }
        />

        <CollapsibleContent>
          <div className="border-t border-[#f0f0f1] px-5 pb-4 pt-1.5">
            <Timeline className="-mx-5 px-5">
              {sorted.map((gen, index) => (
                <TimelineItem key={gen.id}>
                  <TimelineRail>
                    <TimelineIndicator className={DOT_COLOR[gen.status]} />
                    {index < sorted.length - 1 && (
                      <TimelineConnector className="bg-[#e4e4e7]" />
                    )}
                  </TimelineRail>
                  <TimelineContent>
                    <Link
                      key={gen.id}
                      to={`/generated/${spec.id}/versions/${gen.version}`}
                      className="-my-3.5 -mr-5 block border-b border-[#f5f5f6] py-3.5 pr-5 hover:bg-[#fafafa] last:border-b-0"
                    >
                      <div className="flex items-center justify-between gap-3">
                        <div className="flex min-w-0 items-center gap-2.5">
                          <span className="font-mono text-[13px] font-bold">
                            v{gen.version}
                          </span>
                          {gen.version === latest?.version && (
                            <Badge
                              variant="secondary"
                              className="h-auto rounded-full border-[#e4e4e7] bg-[#f4f4f5] px-[7px] py-px text-[10.5px] font-bold uppercase leading-[1.2] tracking-wide text-[#18181b]"
                            >
                              Latest
                            </Badge>
                          )}
                          <span className="truncate text-[12.5px] text-foreground/80">
                            {gen.note}
                          </span>
                        </div>
                        <Badge
                          variant="outline"
                          className={getStatusBadgeClassName(gen.status)}
                        >
                          {getStatusLabel(gen.status)}
                        </Badge>
                      </div>
                      <div className="mt-1.5 flex items-center gap-3.5 text-[12px] text-[#a1a1aa]">
                        <span>{formatDateTime(gen.created)}</span>
                        {gen.durationSeconds != null && (
                          <>
                            <span>·</span>
                            <span>
                              Build time {formatDuration(gen.durationSeconds)}
                            </span>
                          </>
                        )}
                      </div>
                      <div className="mt-2 flex flex-wrap gap-1.5">
                        {[
                          gen.stack.backend,
                          gen.stack.ui,
                          gen.stack.database,
                          gen.stack.infra,
                        ].map((tech) => (
                          <Badge
                            key={tech}
                            variant="secondary"
                            className="h-auto rounded-md border-[#e4e4e7] bg-[#f4f4f5] px-[7px] py-0.5 text-[11px] font-normal leading-[1.2] text-[#3f3f46]"
                          >
                            {tech}
                          </Badge>
                        ))}
                      </div>
                    </Link>
                  </TimelineContent>
                </TimelineItem>
              ))}
            </Timeline>
            <div className="flex justify-end mt-2">
              <Link
                to={`/specifications/${spec.id}`}
                className={cn(
                  buttonVariants({ variant: "outline", size: "sm" }),
                )}
              >
                Open specification
              </Link>
            </div>
          </div>
        </CollapsibleContent>
      </Collapsible>
    </Card>
  );
}
