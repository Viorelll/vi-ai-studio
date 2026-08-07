import { useNavigate } from "react-router-dom";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import { useStartBuild } from "@/hooks/use-builds";
import type { AiModelConfig, TechStack } from "@/lib/types";

const launchSchema = z.object({
  modelId: z.string().min(1, "Select a model"),
});
type LaunchFormValues = z.infer<typeof launchSchema>;

export function LaunchForm({
  specId,
  stack,
  configs,
  defaultConfigId,
}: {
  specId: string;
  stack: TechStack;
  configs: AiModelConfig[];
  defaultConfigId: string | null;
}) {
  const navigate = useNavigate();
  const startBuild = useStartBuild(specId);
  const { control, handleSubmit, watch } = useForm<LaunchFormValues>({
    resolver: zodResolver(launchSchema),
    defaultValues: { modelId: defaultConfigId ?? configs[0]?.id ?? "" },
  });
  const modelId = watch("modelId");

  async function onSubmit({ modelId }: LaunchFormValues) {
    const generation = await startBuild.mutateAsync(modelId || null);
    navigate(`/build/${specId}?generation=${generation.id}`);
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="rounded-2xl border bg-card p-6.5">
      <Label className="text-[12.5px] font-semibold mb-2">Coding model</Label>
      <Controller
        control={control}
        name="modelId"
        render={({ field }) => (
          <Select value={field.value} onValueChange={(value) => field.onChange(value ?? "")}>
            <SelectTrigger className="w-full mb-5.5">
              <SelectValue placeholder="Select a model">
                {() => {
                  const selected = configs.find((config) => config.id === field.value);
                  return selected ? `${selected.label} (${selected.provider})` : "Select a model";
                }}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              {configs.map((config) => (
                <SelectItem key={config.id} value={config.id}>
                  {config.label} ({config.provider})
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
      />

      <div className="flex gap-1.5 flex-wrap mb-6.5">
        {[stack.backend, stack.ui, stack.database, stack.infra].map((tech) => (
          <Badge key={tech} variant="secondary" className="font-normal">
            {tech}
          </Badge>
        ))}
      </div>

      {startBuild.isError && <p className="text-sm text-destructive mb-4">Failed to start the build.</p>}

      <Button type="submit" className="w-full" disabled={startBuild.isPending || !modelId}>
        {startBuild.isPending ? "Starting…" : "Generate"}
      </Button>
    </form>
  );
}
