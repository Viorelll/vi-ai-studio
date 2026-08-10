import type { GenerationStatus, SpecificationStatus } from "@/lib/types";

type Status = SpecificationStatus | GenerationStatus;

const STATUS_STYLES: Record<Status, string> = {
  draft: "bg-zinc-100 text-zinc-600 border-zinc-200",
  building: "bg-blue-50 text-blue-700 border-blue-200",
  running: "bg-blue-50 text-blue-700 border-blue-200",
  ready: "bg-green-50 text-green-700 border-green-200",
  failed: "bg-red-50 text-red-700 border-red-200",
};

const STATUS_LABELS: Record<Status, string> = {
  draft: "Draft",
  building: "Building",
  running: "Running",
  ready: "Ready",
  failed: "Failed",
};

export function getStatusBadgeClassName(status: Status) {
  return `h-auto rounded-full px-2.5 py-[3px] text-[12px] font-semibold leading-[1.2] ${STATUS_STYLES[status]}`;
}

export function getStatusLabel(status: Status) {
  return STATUS_LABELS[status];
}