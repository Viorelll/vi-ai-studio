import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { CheckIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
import { API_BASE_URL } from "@/lib/api-client";
import type { BuildEvent } from "@/lib/types";

const STEPS = [
  { label: "Planning", sub: "Analyzing specification" },
  { label: "Scaffolding", sub: "Setting up the project" },
  { label: "Coding", sub: "Generating implementation" },
  { label: "Tests", sub: "Verifying correctness" },
  { label: "Done", sub: "Build complete" },
];

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
  const logRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const source = new EventSource(`${API_BASE_URL}/api/generations/${generationId}/stream`);

    source.onmessage = (event) => {
      const data: BuildEvent = JSON.parse(event.data);
      setProgressPct(data.progressPct);
      setLogs((prev) => [...prev, { text: data.logLine, color: data.logLine.includes("✓") ? "#4ade80" : "#a1a1aa" }]);
      const stepIndex = STEPS.findIndex((s) => s.label === data.stepLabel);
      if (stepIndex >= 0) setActiveStep(stepIndex);
      if (data.done) {
        setDone(true);
        source.close();
      }
    };

    source.onerror = () => {
      // The stream 500s once the generation's channel is torn down after
      // completion -- harmless if we've already seen a `done` event.
      source.close();
    };

    return () => source.close();
  }, [generationId]);

  useEffect(() => {
    if (logRef.current) logRef.current.scrollTop = logRef.current.scrollHeight;
  }, [logs]);

  return (
    <div className="grid grid-cols-1 sm:grid-cols-[240px_1fr] gap-6">
      <div className="flex flex-col">
        {STEPS.map((step, index) => {
          const isDone = index < activeStep || (index === activeStep && done);
          const isActive = index === activeStep && !done;
          return (
            <div key={step.label} className="flex gap-3">
              <div className="flex flex-col items-center">
                <div
                  className={`size-6 rounded-full flex items-center justify-center text-xs font-bold border ${
                    isDone
                      ? "bg-[var(--brand)] text-white border-[var(--brand)]"
                      : isActive
                        ? "bg-blue-50 text-blue-700 border-blue-200 animate-pulse"
                        : "bg-card text-muted-foreground border-border"
                  }`}
                >
                  {isDone ? <CheckIcon className="size-3.5" /> : isActive ? <span className="size-1.5 rounded-full bg-current" /> : index + 1}
                </div>
                <div className="w-px flex-1 bg-border min-h-7" />
              </div>
              <div className="pb-7">
                <div className={`text-[13px] font-semibold ${isDone || isActive ? "text-foreground" : "text-muted-foreground"}`}>
                  {step.label}
                </div>
                <div className="text-xs text-muted-foreground mt-0.5">{step.sub}</div>
              </div>
            </div>
          );
        })}
      </div>

      <div>
        <div className="flex items-center gap-2.5 mb-3.5">
          <Progress value={progressPct} className="flex-1 h-1.5" />
          <span className="text-xs text-muted-foreground tabular-nums">{progressPct}%</span>
        </div>
        <div
          ref={logRef}
          className="bg-zinc-950 rounded-[10px] p-4 h-[360px] overflow-y-auto font-mono text-[12.5px] leading-relaxed"
        >
          {logs.length === 0 && <div className="text-zinc-500">Waiting for build to start…</div>}
          {logs.map((line, i) => (
            <div key={i} style={{ color: line.color }}>
              {line.text}
            </div>
          ))}
        </div>
        {done && (
          <div className="flex justify-end mt-5">
            <Button onClick={() => navigate("/specifications")}>Done — back to dashboard</Button>
          </div>
        )}
      </div>
    </div>
  );
}
