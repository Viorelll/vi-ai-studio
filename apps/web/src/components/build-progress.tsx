import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { CheckIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
  Timeline,
  TimelineConnector,
  TimelineContent,
  TimelineIndicator,
  TimelineItem,
  TimelineRail,
} from "@/components/ui/timeline";
import { API_BASE_URL, getAuthHeaders } from "@/lib/api-client";
import { clearAuth } from "@/api/auth-token";
import { cn } from "@/lib/utils";
import type { BuildEvent } from "@/lib/types";

// Must match the stage labels BuildOrchestrator emits. The orchestrator sends
// the detail of each step in the log line and only these five as the stage, so
// the timeline always tracks a stage it recognises -- an unknown label would
// silently leave it parked on the previous one for the rest of the build.
const STEPS = [
  { label: "Planning", sub: "Reading the specification" },
  { label: "Generating", sub: "Implementing each phase" },
  { label: "Coverage", sub: "Checking every spec is built" },
  { label: "Verifying", sub: "Compile, test and boot" },
  { label: "Done", sub: "Build complete" },
];

function colorFor(line: string): string {
  if (line.includes("✓")) return "#4ade80";
  if (/\bfailed\b|FAILED/.test(line)) return "#f87171";
  if (line.startsWith("  •") || line.startsWith("  ")) return "#71717a";
  return "#a1a1aa";
}

interface LogLine {
  text: string;
  color: string;
}

export function BuildProgress({ generationId }: { generationId: string }) {
  const navigate = useNavigate();
  const [logs, setLogs] = useState<LogLine[]>([]);
  const [progressPct, setProgressPct] = useState(0);
  const [activeStep, setActiveStep] = useState(0);
  const [done, setDone] = useState(false);
  const logAreaRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const controller = new AbortController();

    const readStream = async () => {
      try {
        const response = await fetch(
          `${API_BASE_URL}/api/generations/${generationId}/stream`,
          { headers: getAuthHeaders(), signal: controller.signal },
        );

        if (response.status === 401) {
          clearAuth();
          return;
        }
        if (!response.ok || !response.body) {
          throw new Error(`Build stream failed with ${response.status}.`);
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = "";

        while (true) {
          const { done: streamDone, value } = await reader.read();
          if (streamDone) break;

          buffer += decoder.decode(value, { stream: true });
          const events = buffer.split("\n\n");
          buffer = events.pop() ?? "";

          for (const event of events) {
            const dataLine = event
              .split("\n")
              .find((line) => line.startsWith("data:"));
            if (!dataLine) continue;

            const data: BuildEvent = JSON.parse(dataLine.slice(5).trim());
            setProgressPct(data.progressPct);
            setLogs((previousLogs) => [
              ...previousLogs,
              { text: data.logLine, color: colorFor(data.logLine) },
            ]);
            const stepIndex = STEPS.findIndex(
              (step) => step.label === data.stepLabel,
            );
            if (stepIndex >= 0) setActiveStep(stepIndex);
            if (data.done) {
              setDone(true);
              controller.abort();
              return;
            }
          }
        }
      } catch (error) {
        if (error instanceof DOMException && error.name === "AbortError")
          return;
      }
    };

    void readStream();
    return () => controller.abort();
  }, [generationId]);

  useEffect(() => {
    const viewport = logAreaRef.current?.querySelector<HTMLElement>(
      '[data-slot="scroll-area-viewport"]',
    );
    if (viewport) viewport.scrollTop = viewport.scrollHeight;
  }, [logs]);

  return (
    <div className="grid grid-cols-1 sm:grid-cols-[240px_1fr] gap-6">
      <Timeline>
        {STEPS.map((step, index) => {
          const isDone = index < activeStep || (index === activeStep && done);
          const isActive = index === activeStep && !done;
          return (
            <TimelineItem
              key={step.label}
              className="grid-cols-[24px_minmax(0,1fr)] pb-7 last:pb-0"
            >
              <TimelineRail className="w-6">
                <TimelineIndicator
                  className={cn(
                    "mt-0 flex size-6 items-center justify-center border-2 text-xs font-bold",
                    isDone
                      ? "border-[var(--brand)] bg-[var(--brand)] text-white"
                      : isActive
                        ? "border-blue-200 bg-blue-50 text-blue-700"
                        : "border-border bg-card text-muted-foreground",
                  )}
                >
                  {isDone ? (
                    <CheckIcon className="size-3.5" />
                  ) : isActive ? (
                    <span className="size-2 rounded-full bg-blue-600" />
                  ) : (
                    index + 1
                  )}
                </TimelineIndicator>
                {index < STEPS.length - 1 && (
                  <TimelineConnector
                    className={cn(
                      "-bottom-7",
                      isDone ? "bg-[var(--brand)]" : "bg-border",
                    )}
                  />
                )}
              </TimelineRail>
              <TimelineContent>
                <div
                  className={cn(
                    "text-[13px] font-semibold",
                    isDone || isActive
                      ? "text-foreground"
                      : "text-muted-foreground",
                  )}
                >
                  {step.label}
                </div>
                <div className="text-xs text-muted-foreground mt-0.5">
                  {step.sub}
                </div>
              </TimelineContent>
            </TimelineItem>
          );
        })}
      </Timeline>

      <div>
        <div className="flex items-center gap-2.5 mb-3.5">
          <Progress value={progressPct} className="flex-1 h-1.5" />
          <span className="text-xs text-muted-foreground tabular-nums">
            {progressPct}%
          </span>
        </div>
        <div ref={logAreaRef}>
          <ScrollArea className="h-[360px] overflow-hidden rounded-[10px] bg-zinc-950">
            <div className="p-4 font-mono text-[12.5px] leading-relaxed">
              {logs.length === 0 && (
                <div className="text-zinc-500">Waiting for build to start…</div>
              )}
              {logs.map((line, i) => (
                // Phase plans and coverage gaps are indented to show nesting,
                // which collapses without pre-wrap.
                <div
                  key={i}
                  className="whitespace-pre-wrap"
                  style={{ color: line.color }}
                >
                  {line.text}
                </div>
              ))}
            </div>
          </ScrollArea>
        </div>
        {done && (
          <div className="flex justify-end mt-5">
            <Button onClick={() => navigate("/specifications")}>
              Done — back to dashboard
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
