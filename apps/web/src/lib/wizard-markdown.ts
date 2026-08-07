import type { SpecificationPhase } from "@/lib/types";

/** Client-side preview only -- mirrors the shape of FinalizeSpecificationHandler's server-side render. */
export function buildPhaseMarkdown(phase: SpecificationPhase): string {
  const lines = [`## Phase ${phase.phaseIndex + 1} — ${phase.title}`, "", `_Produces: ${phase.output}_`, ""];
  for (const item of phase.items) {
    lines.push(`- [${phase.checkedItems.includes(item) ? "x" : " "}] ${item}`);
  }
  if (phase.generatedText?.trim()) {
    lines.push("", phase.generatedText.trim());
  } else if (phase.selectedKeywords.length > 0) {
    lines.push("", `_Keywords: ${phase.selectedKeywords.join(", ")}_`);
  }
  return lines.join("\n");
}
