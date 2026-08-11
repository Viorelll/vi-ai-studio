import { Card } from "@/components/ui/card";

const LAYERS = [
  { label: "Frontend (React + Vite)", tone: "bg-zinc-900" },
  { label: "API (.NET)", tone: "bg-zinc-900" },
  { label: "Application Layer", tone: "bg-zinc-700" },
  { label: "Domain Layer", tone: "bg-zinc-700" },
  { label: "Infrastructure", tone: "bg-zinc-500" },
];

export function StudioArchitectureDiagram() {
  return (
    <Card className="flex flex-col items-center gap-0 rounded-xl bg-muted/30 p-8">
      {LAYERS.map((layer, i) => (
        <div key={layer.label} className="contents">
          <div
            className={`${layer.tone} text-white text-[15px] font-semibold px-6.5 py-3.5 rounded-lg`}
          >
            {layer.label}
          </div>
          {i < LAYERS.length - 1 && (
            <div className="text-lg text-muted-foreground leading-relaxed">
              ▼
            </div>
          )}
        </div>
      ))}
      <div className="text-lg text-muted-foreground leading-relaxed">▼</div>
      <div className="flex gap-4">
        {["PostgreSQL", "Redis", "Blob storage"].map((name) => (
          <div
            key={name}
            className="bg-card border text-foreground/80 text-sm font-semibold px-5 py-3 rounded-lg"
          >
            {name}
          </div>
        ))}
      </div>
    </Card>
  );
}
