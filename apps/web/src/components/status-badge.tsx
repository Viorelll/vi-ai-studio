import { Badge } from "@/components/ui/badge";
import type { GenerationStatus, SpecificationStatus } from "@/lib/types";

const SPEC_STYLES: Record<SpecificationStatus, string> = {
  draft: "bg-zinc-100 text-zinc-600 border-zinc-200",
  building: "bg-blue-50 text-blue-700 border-blue-200",
  ready: "bg-green-50 text-green-700 border-green-200",
  failed: "bg-red-50 text-red-700 border-red-200",
};

const GENERATION_STYLES: Record<GenerationStatus, string> = {
  running: "bg-blue-50 text-blue-700 border-blue-200",
  ready: "bg-green-50 text-green-700 border-green-200",
  failed: "bg-red-50 text-red-700 border-red-200",
};

const LABELS: Record<string, string> = {
  draft: "Draft",
  building: "Building",
  ready: "Ready",
  failed: "Failed",
  running: "Running",
};

export function StatusBadge({ status }: { status: SpecificationStatus | GenerationStatus }) {
  const style = SPEC_STYLES[status as SpecificationStatus] ?? GENERATION_STYLES[status as GenerationStatus];
  return (
    <Badge variant="outline" className={style}>
      {LABELS[status] ?? status}
    </Badge>
  );
}
