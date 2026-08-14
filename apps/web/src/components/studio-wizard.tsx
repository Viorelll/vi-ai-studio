import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { XIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Field, FieldLabel } from "@/components/ui/field";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { StudioChipsStage } from "@/components/studio/studio-chips-stage";
import { StudioInterviewStage } from "@/components/studio/studio-interview-stage";
import { StudioGenerationStage } from "@/components/studio/studio-generation-stage";
import { useDeleteSpecification, useUpdateSpecificationBasics } from "@/hooks/use-specifications";
import { useIntakeSheet } from "@/hooks/use-specification-intake";
import type { SpecificationDetail } from "@/lib/types";

type Stage = "chips" | "interview" | "generate";

/**
 * Thin stage router for the specification authoring pipeline: chip
 * selection -> domain interview -> batch generation. Which stage is active
 * derives from the intake sheet's completion flags; `stageOverride` only
 * lets the user revisit an already-reached stage via the stepper.
 */
export function StudioWizard({ spec }: { spec: SpecificationDetail }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const updateBasics = useUpdateSpecificationBasics(spec.id);
  const deleteSpecification = useDeleteSpecification();
  const intakeQuery = useIntakeSheet(spec.id);

  const [name, setName] = useState(spec.name);
  const [summary, setSummary] = useState(spec.summary);
  const [leaving, setLeaving] = useState(false);
  const [stageOverride, setStageOverride] = useState<Stage | null>(null);

  const nameMissing = name.trim().length === 0;
  const summaryMissing = summary.trim().length === 0;
  const basicsIncomplete = nameMissing || summaryMissing;

  const isUntitled = name.trim() === "" || name.trim() === "Untitled Project";
  const nameRef = useRef(name);
  nameRef.current = name;
  const handledLeaveRef = useRef(false);
  const discardTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  async function leaveStudio(destination: string, untitledDestination = destination) {
    if (!isUntitled) {
      navigate(destination);
      return;
    }
    handledLeaveRef.current = true;
    setLeaving(true);
    try {
      await deleteSpecification.mutateAsync(spec.id);
      navigate(untitledDestination);
    } catch {
      handledLeaveRef.current = false;
      navigate(destination);
    } finally {
      setLeaving(false);
    }
  }

  // Catches every other way of leaving the wizard (header/nav links, browser
  // back/forward, sign-out) that don't go through leaveStudio() above. The
  // setTimeout + clear-on-remount dance is needed because React StrictMode
  // double-invokes this effect (mount -> cleanup -> mount) in development;
  // without it, the freshly-created spec gets deleted immediately after
  // "New Project" is clicked.
  useEffect(() => {
    if (discardTimeoutRef.current !== null) {
      clearTimeout(discardTimeoutRef.current);
      discardTimeoutRef.current = null;
    }
    return () => {
      if (handledLeaveRef.current) return;
      discardTimeoutRef.current = setTimeout(() => {
        const current = nameRef.current.trim();
        if (current === "" || current === "Untitled Project") {
          deleteSpecification.mutate(spec.id);
        }
      }, 0);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function saveBasics() {
    updateBasics.mutate({ name, summary });
  }

  function refreshIntake() {
    queryClient.invalidateQueries({ queryKey: ["specifications", spec.id, "intake"] });
    setStageOverride(null);
  }

  if (intakeQuery.isPending) {
    return <div className="p-10 text-sm text-muted-foreground">Loading…</div>;
  }

  const intake = intakeQuery.data ?? null;
  const chipsDone = Boolean(intake?.completedAt);
  const interviewDone = Boolean(intake?.interviewCompletedAt);
  const derivedStage: Stage = !chipsDone ? "chips" : !interviewDone ? "interview" : "generate";
  const stage = stageOverride ?? derivedStage;

  const steps: { key: Stage; label: string; reached: boolean }[] = [
    { key: "chips", label: "1. Shape", reached: true },
    { key: "interview", label: "2. Interview", reached: chipsDone },
    { key: "generate", label: "3. Generate", reached: interviewDone },
  ];

  return (
    <div className="w-full max-w-225 mx-auto">
      <div className="flex items-center justify-between mb-4">
        <div>
          <PageBreadcrumb items={[{ label: "AI Specification Studio" }]} />
          <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">
            AI Specification Studio
          </div>
        </div>
        <Tooltip>
          <TooltipTrigger
            render={
              <Button
                variant="outline"
                size="icon"
                onClick={() => leaveStudio("/")}
                disabled={leaving}
                aria-label="Discard changes and return home"
              >
                <XIcon />
              </Button>
            }
          />
          <TooltipContent>Discard changes and return home</TooltipContent>
        </Tooltip>
      </div>

      <div className="flex items-center gap-2 mb-6">
        {steps.map((s) => {
          const active = stage === s.key;
          return (
            <button
              key={s.key}
              type="button"
              disabled={!s.reached}
              onClick={() => s.reached && setStageOverride(s.key)}
              className={`text-xs font-semibold px-3 py-1.5 rounded-full border transition-colors ${
                active
                  ? "bg-[var(--brand)] text-white border-[var(--brand)]"
                  : s.reached
                    ? "border-[#e4e4e7] text-[#3f3f46] hover:bg-[#f4f4f5]"
                    : "border-[#e4e4e7] text-[#a1a1aa] cursor-not-allowed"
              }`}
            >
              {s.label}
            </button>
          );
        })}
      </div>

      <Card className="gap-0 rounded-[12px] p-4 mb-5">
        <div className="text-[13px] font-semibold mb-2.5">Project basics</div>
        <div className="grid grid-cols-2 gap-2.5">
          <Field className="gap-1.5">
            <FieldLabel htmlFor="studio-project-name" className="sr-only">
              Project name (required)
            </FieldLabel>
            <Input
              id="studio-project-name"
              placeholder="Project name *"
              value={name}
              onChange={(e) => setName(e.target.value)}
              onBlur={saveBasics}
              required
              aria-invalid={nameMissing}
            />
          </Field>
          <Field className="gap-1.5">
            <FieldLabel htmlFor="studio-project-summary" className="sr-only">
              One-line summary (required)
            </FieldLabel>
            <Input
              id="studio-project-summary"
              placeholder="One-line summary *"
              value={summary}
              onChange={(e) => setSummary(e.target.value)}
              onBlur={saveBasics}
              required
              aria-invalid={summaryMissing}
            />
          </Field>
        </div>
        {basicsIncomplete && (
          <p className="text-[11px] text-destructive mt-2">
            Name and summary are required to start the wizard.
          </p>
        )}
      </Card>

      {basicsIncomplete ? (
        <div className="rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-2.5 text-[12.5px] text-amber-800">
          Set a project name and summary above to unlock this wizard.
        </div>
      ) : stage === "chips" ? (
        <StudioChipsStage specId={spec.id} intakeSheet={intake} onSaved={refreshIntake} />
      ) : stage === "interview" ? (
        <StudioInterviewStage specId={spec.id} onCompleted={refreshIntake} />
      ) : (
        <StudioGenerationStage specId={spec.id} />
      )}
    </div>
  );
}
