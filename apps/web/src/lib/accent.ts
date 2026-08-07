export type AccentKey = "slate" | "indigo" | "emerald" | "amber" | "rose";

export interface AccentDefinition {
  label: string;
  color: string;
  tint: string;
  tintBorder: string;
}

// Matches design/README.md's "Team accent colors" table exactly.
export const ACCENT_COLORS: Record<AccentKey, AccentDefinition> = {
  slate: { label: "Slate", color: "#18181b", tint: "#f4f4f5", tintBorder: "#e4e4e7" },
  indigo: { label: "Indigo", color: "#4f46e5", tint: "#eef0fd", tintBorder: "#d7dafa" },
  emerald: { label: "Emerald", color: "#059669", tint: "#eefbf4", tintBorder: "#cdeedd" },
  amber: { label: "Amber", color: "#b45309", tint: "#fdf3e7", tintBorder: "#f2ddb8" },
  rose: { label: "Rose", color: "#e11d48", tint: "#fdedf0", tintBorder: "#f8ccd5" },
};

export const ACCENT_KEYS = Object.keys(ACCENT_COLORS) as AccentKey[];
export const DEFAULT_ACCENT: AccentKey = "slate";
