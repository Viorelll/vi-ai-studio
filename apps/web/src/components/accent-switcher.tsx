import { useAccentStore } from "@/store/accent-store";
import { ACCENT_COLORS, ACCENT_KEYS, type AccentKey } from "@/lib/accent";
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";

export function AccentSwitcher() {
  const accentKey = useAccentStore((s) => s.accentKey);
  const setAccentKey = useAccentStore((s) => s.setAccentKey);

  return (
    <ToggleGroup
      multiple={false}
      value={[accentKey]}
      onValueChange={(values) => {
        const value = values[0];
        if (value) setAccentKey(value as AccentKey);
      }}
      spacing={0}
      className="mx-1 gap-[5px] rounded-none border-x px-1"
      aria-label="Accent color"
    >
      {ACCENT_KEYS.map((key) => {
        const swatch = ACCENT_COLORS[key];
        const active = key === accentKey;
        return (
          <Tooltip key={key}>
            <TooltipTrigger
              render={
                <ToggleGroupItem
                  value={key}
                  aria-label={`Select ${swatch.label} accent`}
                  className="size-[18px] rounded-full border-0 bg-transparent p-0 transition hover:scale-[1.15] hover:bg-transparent"
                  style={{
                    backgroundColor: swatch.color,
                    boxShadow: `0 0 0 2px var(--background), 0 0 0 3.5px ${active ? swatch.color : "transparent"}`,
                  }}
                />
              }
            />
            <TooltipContent>{swatch.label}</TooltipContent>
          </Tooltip>
        );
      })}
    </ToggleGroup>
  );
}
