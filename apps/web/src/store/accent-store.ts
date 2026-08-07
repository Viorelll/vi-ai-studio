import { create } from "zustand";
import { persist } from "zustand/middleware";
import { ACCENT_COLORS, DEFAULT_ACCENT, type AccentKey } from "@/lib/accent";

function applyAccent(key: AccentKey) {
  const { color, tint, tintBorder } = ACCENT_COLORS[key];
  const root = document.documentElement.style;
  // Remap shadcn's --primary to the chosen accent so Button/Badge/Progress
  // default variants pick it up for free; --brand-tint(-border) back the
  // design's tinted panels (e.g. the Generated Projects stat card).
  root.setProperty("--primary", color);
  root.setProperty("--primary-foreground", "#ffffff");
  root.setProperty("--brand", color);
  root.setProperty("--brand-tint", tint);
  root.setProperty("--brand-tint-border", tintBorder);
}

interface AccentState {
  accentKey: AccentKey;
  setAccentKey: (key: AccentKey) => void;
}

export const useAccentStore = create<AccentState>()(
  persist(
    (set) => ({
      accentKey: DEFAULT_ACCENT,
      setAccentKey: (key) => {
        applyAccent(key);
        set({ accentKey: key });
      },
    }),
    {
      name: "vi-ai-studio-accent",
      // Only the color key is persisted -- CSS vars are a render side effect,
      // re-applied on rehydration below rather than stored themselves.
      partialize: (state) => ({ accentKey: state.accentKey }),
      onRehydrateStorage: () => (state) => {
        if (state) applyAccent(state.accentKey);
      },
    },
  ),
);
