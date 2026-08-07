import { useAccentStore } from "@/store/accent-store";
import { ACCENT_COLORS, ACCENT_KEYS } from "@/lib/accent";

export function AccentSwitcher() {
  const accentKey = useAccentStore((s) => s.accentKey);
  const setAccentKey = useAccentStore((s) => s.setAccentKey);

  return (
    <div className="flex items-center gap-[5px] mx-1 px-1 border-x">
      {ACCENT_KEYS.map((key) => {
        const swatch = ACCENT_COLORS[key];
        const active = key === accentKey;
        return (
          <button
            key={key}
            type="button"
            title={swatch.label}
            aria-pressed={active}
            onClick={() => setAccentKey(key)}
            className="size-[18px] rounded-full transition hover:scale-[1.15]"
            style={{
              backgroundColor: swatch.color,
              boxShadow: `0 0 0 2px var(--background), 0 0 0 3.5px ${active ? swatch.color : "transparent"}`,
            }}
          />
        );
      })}
    </div>
  );
}
