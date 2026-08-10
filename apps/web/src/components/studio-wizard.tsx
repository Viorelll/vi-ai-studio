import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { XIcon, CheckIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Field, FieldLabel } from "@/components/ui/field";
import { Progress } from "@/components/ui/progress";
import { Checkbox } from "@/components/ui/checkbox";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Separator } from "@/components/ui/separator";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { StudioArchitectureDiagram } from "@/components/studio-architecture-diagram";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { KEYWORD_BANK } from "@/lib/keyword-bank";
import { buildPhaseMarkdown } from "@/lib/wizard-markdown";
import {
  useDeleteSpecification,
  useFinalizeSpecification,
  useGeneratePhaseText,
  useSaveSpecificationPhase,
  useUpdateSpecificationBasics,
} from "@/hooks/use-specifications";
import type {
  SpecificationDetail,
  SpecificationPhase,
  TechStack,
} from "@/lib/types";

const TECHNICAL_DESIGN_PHASE_INDEX = 7;

const STACK_FIELDS: {
  key: keyof TechStack;
  label: string;
  options: { value: string; disabled?: boolean }[];
}[] = [
  {
    key: "backend",
    label: "Backend",
    options: [
      { value: ".NET Web API" },
      { value: "Python (coming soon)", disabled: true },
      { value: "Java (coming soon)", disabled: true },
    ],
  },
  {
    key: "ui",
    label: "UI framework",
    options: [
      { value: "Next.js" },
      { value: "Angular (coming soon)", disabled: true },
      { value: "Blazor (coming soon)", disabled: true },
    ],
  },
  {
    key: "database",
    label: "Database",
    options: [
      { value: "PostgreSQL" },
      { value: "SQL Server (coming soon)", disabled: true },
    ],
  },
  {
    key: "infra",
    label: "Containerization",
    options: [
      { value: "Docker" },
      { value: "Kubernetes (coming soon)", disabled: true },
    ],
  },
  {
    key: "uiStyle",
    label: "UI style",
    options: [
      { value: "Tailwind" },
      { value: "Bootstrap (coming soon)", disabled: true },
    ],
  },
];

export function StudioWizard({ spec }: { spec: SpecificationDetail }) {
  const navigate = useNavigate();
  const updateBasics = useUpdateSpecificationBasics(spec.id);
  const savePhase = useSaveSpecificationPhase(spec.id);
  const generatePhaseText = useGeneratePhaseText(spec.id);
  const finalizeSpecification = useFinalizeSpecification(spec.id);
  const deleteSpecification = useDeleteSpecification();

  const [name, setName] = useState(spec.name);
  const [summary, setSummary] = useState(spec.summary);
  const [stack, setStack] = useState<TechStack>(spec.stack);
  const [phases, setPhases] = useState<SpecificationPhase[]>(
    [...spec.phases].sort((a, b) => a.phaseIndex - b.phaseIndex),
  );
  const [activePhaseIndex, setActivePhaseIndex] = useState(0);
  const [activeItemIndex, setActiveItemIndex] = useState(0);
  const [finalizing, setFinalizing] = useState(false);
  const [leaving, setLeaving] = useState(false);

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

  const activePhase = phases[activePhaseIndex];
  const activeItem = activePhase.items[activeItemIndex] as string | undefined;

  const completedPhaseCount = useMemo(
    () =>
      phases.filter(
        (p) => p.items.length > 0 && p.checkedItems.length === p.items.length,
      ).length,
    [phases],
  );
  const wizardProgressPct =
    phases.length === 0
      ? 0
      : Math.round((completedPhaseCount / phases.length) * 100);

  function updatePhase(index: number, patch: Partial<SpecificationPhase>) {
    setPhases((prev) => {
      const next = prev.map((p, i) => (i === index ? { ...p, ...patch } : p));
      const phase = next[index];
      savePhase.mutate({
        phaseIndex: phase.phaseIndex,
        checkedItems: phase.checkedItems,
        selectedKeywords: phase.selectedKeywords,
      });
      return next;
    });
  }

  function toggleCurrentItem() {
    if (!activeItem) return;
    const checked = activePhase.checkedItems.includes(activeItem)
      ? activePhase.checkedItems.filter((i) => i !== activeItem)
      : [...activePhase.checkedItems, activeItem];
    updatePhase(activePhaseIndex, { checkedItems: checked });
  }

  function toggleKeyword(keyword: string) {
    const selected = activePhase.selectedKeywords.includes(keyword)
      ? activePhase.selectedKeywords.filter((k) => k !== keyword)
      : [...activePhase.selectedKeywords, keyword];
    updatePhase(activePhaseIndex, {
      selectedKeywords: selected,
      generatedText: null,
    });
  }

  function jumpPhase(index: number) {
    setActivePhaseIndex(index);
    setActiveItemIndex(0);
  }

  async function generate() {
    try {
      const updated = await generatePhaseText.mutateAsync(
        activePhase.phaseIndex,
      );
      updatePhase(activePhaseIndex, { generatedText: updated.generatedText });
    } catch {
      // Non-fatal in the wizard -- the phase just keeps its checklist/keyword preview.
    }
  }

  function saveBasics() {
    updateBasics.mutate({ name, summary, stack });
  }

  function updateStackField(key: keyof TechStack, value: string) {
    const next = { ...stack, [key]: value };
    setStack(next);
    updateBasics.mutate({ stack: next });
  }

  async function back() {
    if (activeItemIndex > 0) {
      setActiveItemIndex(activeItemIndex - 1);
      return;
    }
    if (activePhaseIndex === 0) {
      await leaveStudio(`/specifications/${spec.id}`, "/specifications");
      return;
    }
    const prevIndex = activePhaseIndex - 1;
    setActivePhaseIndex(prevIndex);
    setActiveItemIndex(Math.max(0, phases[prevIndex].items.length - 1));
  }

  async function next() {
    if (activeItemIndex < activePhase.items.length - 1) {
      setActiveItemIndex(activeItemIndex + 1);
      return;
    }
    if (activePhaseIndex < phases.length - 1) {
      setActivePhaseIndex(activePhaseIndex + 1);
      setActiveItemIndex(0);
      return;
    }
    setFinalizing(true);
    try {
      await finalizeSpecification.mutateAsync();
      navigate(`/specifications/${spec.id}`);
    } finally {
      setFinalizing(false);
    }
  }

  const isLastStep =
    activePhaseIndex === phases.length - 1 &&
    activeItemIndex === activePhase.items.length - 1;

  return (
    <div className="w-full max-w-[1160px] mx-auto">
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

      <div className="flex items-center gap-3 mb-6">
        <Progress value={wizardProgressPct} className="flex-1 h-1.5" />
        <span className="text-xs text-muted-foreground whitespace-nowrap">
          {completedPhaseCount} / {phases.length} phases complete
        </span>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[270px_1fr] gap-6 items-start">
        <Card className="gap-0 rounded-[12px] p-4">
          <div className="text-[13px] font-semibold mb-2.5">Project basics</div>
          <div className="flex flex-col gap-2.5 mb-4">
            <Field className="gap-1.5">
              <FieldLabel htmlFor="studio-project-name" className="sr-only">
                Project name
              </FieldLabel>
              <Input
                id="studio-project-name"
                placeholder="Project name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                onBlur={saveBasics}
              />
            </Field>
            <Field className="gap-1.5">
              <FieldLabel htmlFor="studio-project-summary" className="sr-only">
                One-line summary
              </FieldLabel>
              <Input
                id="studio-project-summary"
                placeholder="One-line summary"
                value={summary}
                onChange={(e) => setSummary(e.target.value)}
                onBlur={saveBasics}
              />
            </Field>
            {STACK_FIELDS.map((field) => (
              <Field key={field.key} className="gap-1">
                <FieldLabel
                  htmlFor={`studio-stack-${field.key}`}
                  className="text-[11px] text-muted-foreground"
                >
                  {field.label}
                </FieldLabel>
                <Select
                  value={stack[field.key]}
                  onValueChange={(v) => v && updateStackField(field.key, v)}
                >
                  <SelectTrigger
                    id={`studio-stack-${field.key}`}
                    className="w-full"
                  >
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {field.options.map((opt) => (
                      <SelectItem
                        key={opt.value}
                        value={opt.value}
                        disabled={opt.disabled}
                      >
                        {opt.value}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </Field>
            ))}
          </div>

          <Separator className="mb-3" />
          <div className="text-[11px] font-semibold text-[#a1a1aa] uppercase tracking-wide mb-2">
            15 phases
          </div>
          <ScrollArea className="h-[520px]">
            <div className="flex flex-col gap-0.5 pr-2">
              {phases.map((phase, index) => {
                const complete =
                  phase.checkedItems.length === phase.items.length &&
                  phase.items.length > 0;
                const active = index === activePhaseIndex;
                return (
                  <Button
                    key={phase.phaseIndex}
                    type="button"
                    variant="ghost"
                    onClick={() => jumpPhase(index)}
                    className={`h-auto w-full justify-start gap-2.5 rounded-lg px-2 py-2 text-left hover:bg-[#f4f4f5] ${active ? "bg-[#f4f4f5]" : ""}`}
                    aria-current={active ? "step" : undefined}
                  >
                    {/*
                      The design hardcodes the completed-checkmark circle to
                      #18181b regardless of the chosen team accent -- only
                      primary actions (buttons, progress bars) track the
                      accent, structural status dots stay a fixed dark neutral.
                    */}
                    <span
                      className={`text-[10.5px] font-bold size-5 rounded-full shrink-0 flex items-center justify-center border ${
                        complete
                          ? "bg-[#18181b] text-white border-[#18181b]"
                          : active
                            ? "bg-[#eff6ff] text-[#1d4ed8] border-[#bfdbfe]"
                            : "bg-white text-[#a1a1aa] border-[#e4e4e7]"
                      }`}
                    >
                      {complete ? <CheckIcon className="size-3" /> : index + 1}
                    </span>
                    <span
                      className={`text-[12.5px] font-medium flex-1 ${active ? "text-[#18181b]" : "text-[#52525b]"}`}
                    >
                      {phase.title}
                    </span>
                    <span className="text-[11px] text-[#a1a1aa]">
                      {phase.checkedItems.length}/{phase.items.length}
                    </span>
                  </Button>
                );
              })}
            </div>
          </ScrollArea>
        </Card>

        <Card className="gap-0 rounded-[12px] p-7">
          <div className="text-xs font-semibold text-[#71717a] uppercase tracking-wide mb-1">
            Phase {activePhaseIndex + 1} of {phases.length}
          </div>
          <h2 className="text-xl font-bold tracking-tight mb-1.5">
            {activePhase.title}
          </h2>
          <p className="text-sm text-[#71717a] mb-5">
            Produces: {activePhase.output}
          </p>

          <div className="flex gap-1.5 mb-3.5">
            {activePhase.items.map((item, itemIdx) => {
              const checked = activePhase.checkedItems.includes(item);
              return (
                <div
                  key={item}
                  className={`h-1 flex-1 rounded-full ${
                    checked
                      ? "bg-[var(--brand)]"
                      : itemIdx === activeItemIndex
                        ? "bg-[#93c5fd]"
                        : "bg-[#e4e4e7]"
                  }`}
                />
              );
            })}
          </div>

          <div className="text-[11px] font-semibold text-[#a1a1aa] uppercase tracking-wide mb-2.5">
            Step {activeItemIndex + 1} of {activePhase.items.length}
          </div>

          {activeItem && (
            <Label
              htmlFor={`studio-item-${activePhase.phaseIndex}-${activeItemIndex}`}
              className="flex w-full cursor-pointer items-center gap-3.5 rounded-lg border border-[#e4e4e7] p-5 text-left hover:border-[#a1a1aa]"
            >
              <Checkbox
                id={`studio-item-${activePhase.phaseIndex}-${activeItemIndex}`}
                checked={activePhase.checkedItems.includes(activeItem)}
                onCheckedChange={toggleCurrentItem}
                aria-label={`Mark ${activeItem} complete`}
                className={`size-6.5 rounded-md text-white ${
                  activePhase.checkedItems.includes(activeItem)
                    ? "border-[var(--brand)] bg-[var(--brand)]"
                    : "border-[#d4d4d8] bg-white"
                }`}
              />
              <span className="text-base font-semibold">{activeItem}</span>
            </Label>
          )}

          <Label className="text-[13px] font-semibold mb-2">Keywords</Label>
          <div className="flex flex-wrap gap-1.5 mb-5">
            {/* Selected keyword chips are hardcoded dark like the phase circles above, not accent-driven. */}
            {(KEYWORD_BANK[activePhase.phaseIndex] ?? []).map((keyword) => {
              const selected = activePhase.selectedKeywords.includes(keyword);
              return (
                <Button
                  key={keyword}
                  type="button"
                  variant="outline"
                  onClick={() => toggleKeyword(keyword)}
                  className={`h-auto rounded-full px-3 py-1.5 text-[12.5px] font-medium whitespace-nowrap ${
                    selected
                      ? "border-[#18181b] bg-[#18181b] text-white hover:bg-[#18181b] hover:text-white"
                      : "border-[#e4e4e7] bg-white text-[#3f3f46] hover:bg-[#f4f4f5]"
                  }`}
                  aria-pressed={selected}
                >
                  {keyword}
                </Button>
              );
            })}
          </div>

          {activePhase.phaseIndex === TECHNICAL_DESIGN_PHASE_INDEX && (
            <div className="mb-5">
              <Label className="text-[13px] font-semibold mb-2.5">
                Architecture diagram
              </Label>
              <StudioArchitectureDiagram />
            </div>
          )}

          <div className="flex items-center justify-between mb-1.5">
            <Label className="text-[13px] font-semibold">Preview (.md)</Label>
            <Button
              size="sm"
              onClick={generate}
              disabled={generatePhaseText.isPending}
            >
              {generatePhaseText.isPending ? "Generating…" : "Generate"}
            </Button>
          </div>
          <ScrollArea className="h-[220px] w-full rounded-lg border bg-muted/30">
            <pre className="p-3.5 text-[12.5px] font-mono leading-relaxed text-foreground/80 whitespace-pre-wrap">
              {buildPhaseMarkdown(activePhase)}
            </pre>
          </ScrollArea>

          <div className="flex justify-between mt-6">
            <Button variant="outline" onClick={back} disabled={leaving}>
              {leaving ? "Discarding…" : "Back"}
            </Button>
            <Button onClick={next} disabled={finalizing || leaving}>
              {finalizing ? "Finalizing…" : isLastStep ? "Finish" : "Next"}
            </Button>
          </div>
        </Card>
      </div>
    </div>
  );
}
