import * as React from "react";

import { cn } from "@/lib/utils";

function Timeline({ className, ...props }: React.ComponentProps<"ol">) {
  return (
    <ol
      data-slot="timeline"
      className={cn("flex flex-col", className)}
      {...props}
    />
  );
}

function TimelineItem({ className, ...props }: React.ComponentProps<"li">) {
  return (
    <li
      data-slot="timeline-item"
      className={cn(
        "relative grid grid-cols-[10px_minmax(0,1fr)] gap-2.5 pb-3.5 last:pb-0",
        className,
      )}
      {...props}
    />
  );
}

function TimelineRail({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="timeline-rail"
      className={cn("relative flex min-h-full w-2.5 justify-center", className)}
      {...props}
    />
  );
}

function TimelineIndicator({
  className,
  ...props
}: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="timeline-indicator"
      className={cn(
        "relative z-10 mt-1 size-[9px] shrink-0 rounded-full border-2 border-white bg-background ring-1 ring-border",
        className,
      )}
      {...props}
    />
  );
}

function TimelineConnector({
  className,
  ...props
}: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="timeline-connector"
      aria-hidden="true"
      className={cn(
        "absolute bottom-[-14px] left-1/2 top-3 w-px -translate-x-1/2 bg-border",
        className,
      )}
      {...props}
    />
  );
}

function TimelineContent({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="timeline-content"
      className={cn("min-w-0", className)}
      {...props}
    />
  );
}

export {
  Timeline,
  TimelineItem,
  TimelineRail,
  TimelineIndicator,
  TimelineConnector,
  TimelineContent,
};
