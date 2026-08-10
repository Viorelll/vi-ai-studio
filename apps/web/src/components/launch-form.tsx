import { useNavigate } from "react-router-dom";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { CircleAlertIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Field, FieldLabel } from "@/components/ui/field";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
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
    <Card className="gap-0 rounded-[14px] bg-white p-0">
      <form onSubmit={handleSubmit(onSubmit)} className="p-6.5">
        <Field className="mb-5.5 gap-2">
          <FieldLabel
            htmlFor="launch-model"
            className="text-[12.5px] font-semibold"
          >
            Coding model
          </FieldLabel>
          <Controller
            control={control}
            name="modelId"
            render={({ field }) => (
              <Select
                value={field.value}
                onValueChange={(value) => field.onChange(value ?? "")}
              >
                <SelectTrigger
                  id="launch-model"
                  className="h-11 w-full rounded-lg border-[#e4e4e7] text-sm"
                >
                  <SelectValue placeholder="Select a model">
                    {() => {
                      const selected = configs.find(
                        (config) => config.id === field.value,
                      );
                      return selected
                        ? `${selected.label} (${selected.provider})`
                        : "Select a model";
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
        </Field>

        <div className="flex gap-1.5 flex-wrap mb-6.5">
          {[stack.backend, stack.ui, stack.database, stack.infra].map(
            (tech) => (
              <Badge
                key={tech}
                variant="secondary"
                className="rounded-md border-[#e4e4e7] bg-[#f4f4f5] px-2 py-0.5 text-[12px] font-normal text-[#3f3f46]"
              >
                {tech}
              </Badge>
            ),
          )}
        </div>

        {startBuild.isError && (
          <Alert variant="destructive" className="mb-4">
            <CircleAlertIcon />
            <AlertTitle>Unable to start AI Build</AlertTitle>
            <AlertDescription>
              Failed to start the build. Check the selected model configuration
              and try again.
            </AlertDescription>
          </Alert>
        )}

        <Button
          type="submit"
          className="h-10 w-full rounded-lg text-sm font-bold"
          disabled={startBuild.isPending || !modelId}
        >
          {startBuild.isPending ? "Starting…" : "Generate"}
        </Button>
      </form>
    </Card>
  );
}
