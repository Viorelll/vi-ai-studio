import { useEffect, useState } from "react";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { useChipGroups, useSaveIntakeChips } from "@/hooks/use-specification-intake";
import type { ChipGroup, IntakeSheet } from "@/lib/types";
import { ApiError } from "@/lib/api-client";

type Selections = Record<string, string | string[]>;

function defaultSelections(groups: ChipGroup[]): Selections {
  const selections: Selections = {};
  for (const group of groups) {
    const defaults = group.options.filter((o) => o.isDefault).map((o) => o.value);
    selections[group.sheetField] = group.selectMode === "single" ? (defaults[0] ?? group.options[0]?.value ?? "") : defaults;
  }
  return selections;
}

function selectionsFromSheet(groups: ChipGroup[], sheet: IntakeSheet): Selections {
  const bySheetField: Record<string, unknown> = sheet;
  const selections: Selections = {};
  for (const group of groups) {
    const value = bySheetField[group.sheetField];
    selections[group.sheetField] =
      group.selectMode === "single" ? (typeof value === "string" && value ? value : defaultSelections([group])[group.sheetField]) : Array.isArray(value) ? value : [];
  }
  return selections;
}

export function StudioChipsStage({
  specId,
  intakeSheet,
  onSaved,
}: {
  specId: string;
  intakeSheet: IntakeSheet | null;
  onSaved: () => void;
}) {
  const chipGroupsQuery = useChipGroups(specId);
  const saveChips = useSaveIntakeChips(specId);
  const [selections, setSelections] = useState<Selections>({});
  const [initialized, setInitialized] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!chipGroupsQuery.data || initialized) return;
    setSelections(intakeSheet ? selectionsFromSheet(chipGroupsQuery.data, intakeSheet) : defaultSelections(chipGroupsQuery.data));
    setInitialized(true);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [chipGroupsQuery.data]);

  if (chipGroupsQuery.isPending || !initialized) {
    return (
      <div className="flex items-center gap-2 p-10 text-sm text-muted-foreground">
        <Spinner />
        Loading…
      </div>
    );
  }
  if (chipGroupsQuery.isError) {
    return <div className="p-10 text-sm text-destructive">Couldn't load the option groups.</div>;
  }

  function toggle(group: ChipGroup, value: string) {
    setSelections((prev) => {
      if (group.selectMode === "single") {
        return { ...prev, [group.sheetField]: value };
      }
      const current = (prev[group.sheetField] as string[]) ?? [];
      const next = current.includes(value) ? current.filter((v) => v !== value) : [...current, value];
      return { ...prev, [group.sheetField]: next };
    });
  }

  function isSelected(group: ChipGroup, value: string) {
    const current = selections[group.sheetField];
    return group.selectMode === "single" ? current === value : Array.isArray(current) && current.includes(value);
  }

  function handleContinue() {
    setError(null);
    const body = selections as unknown as Parameters<ReturnType<typeof useSaveIntakeChips>["mutate"]>[0];
    saveChips.mutate(body, {
      onSuccess: () => onSaved(),
      onError: (err) => setError(err instanceof ApiError ? err.message : "Couldn't save your selections."),
    });
  }

  return (
    <div className="flex flex-col gap-5">
      <div>
        <h2 className="text-xl font-bold tracking-tight mb-1.5">Shape your product</h2>
        <p className="text-sm text-muted-foreground">
          Fifteen quick choices that fix what the specification covers. Every group has a sensible
          default already selected — change only what matters for this product.
        </p>
      </div>

      <div className="flex flex-col gap-3">
        {chipGroupsQuery.data.map((group) => (
          <Card key={group.group} className="rounded-[12px] p-5 gap-2">
            <div className="flex items-baseline justify-between gap-3">
              <h3 className="text-sm font-semibold">{group.label}</h3>
              <span className="text-[11px] text-muted-foreground shrink-0">{group.changes}</span>
            </div>
            <div className="flex flex-wrap gap-1.5 mt-1">
              {group.options.map((option) => {
                const selected = isSelected(group, option.value);
                return (
                  <Button
                    key={option.value}
                    type="button"
                    variant="outline"
                    onClick={() => toggle(group, option.value)}
                    className={`h-auto rounded-full px-3 py-1.5 text-[12.5px] font-medium whitespace-nowrap ${
                      selected
                        ? "border-[var(--brand)] bg-[var(--brand)] text-white hover:bg-[var(--brand)] hover:text-white"
                        : "border-[#e4e4e7] bg-white text-[#3f3f46] hover:bg-[#f4f4f5]"
                    }`}
                    aria-pressed={selected}
                  >
                    {option.value}
                  </Button>
                );
              })}
            </div>
          </Card>
        ))}
      </div>

      {error && <p className="text-[12.5px] text-destructive">{error}</p>}

      <div className="flex justify-end">
        <Button onClick={handleContinue} disabled={saveChips.isPending}>
          {saveChips.isPending ? "Saving…" : "Continue to interview"}
        </Button>
      </div>
    </div>
  );
}
