import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { CircleAlertIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Field, FieldDescription, FieldLabel } from "@/components/ui/field";
import { Empty, EmptyDescription } from "@/components/ui/empty";
import { ButtonGroup } from "@/components/ui/button-group";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogMedia,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { PROVIDERS, TASK_DEFS } from "@/lib/ai-task-defs";
import {
  useAiConfigs,
  useCreateAiConfig,
  useDeleteAiConfig,
  useRevealAiConfigKey,
  useTaskRouting,
  useUpdateAiConfig,
  useUpdateTaskRouting,
  type AiConfigFormInput,
} from "@/hooks/use-admin";
import type { AiModelConfig, AiProvider } from "@/lib/types";

const configFormSchema = z.object({
  label: z.string().min(1, "Label is required"),
  provider: z.string().min(1) as z.ZodType<AiProvider>,
  model: z.string().min(1, "Model name is required"),
  baseUrl: z.string(),
  apiKey: z.string(),
});

const EMPTY_FORM: AiConfigFormInput = {
  label: "",
  provider: "openAi",
  model: "",
  baseUrl: "",
  apiKey: "",
};

export function AiConfigManager() {
  const configsQuery = useAiConfigs();
  const taskRoutingQuery = useTaskRouting();
  const configs = configsQuery.data ?? [];
  const routing = taskRoutingQuery.data ?? [];

  const createConfig = useCreateAiConfig();
  const updateConfig = useUpdateAiConfig();
  const deleteConfig = useDeleteAiConfig();
  const revealKey = useRevealAiConfigKey();
  const updateRouting = useUpdateTaskRouting();

  const [revealed, setRevealed] = useState<Record<string, string>>({});
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [configToDelete, setConfigToDelete] = useState<AiModelConfig | null>(
    null,
  );

  const { control, handleSubmit, reset } = useForm<AiConfigFormInput>({
    resolver: zodResolver(configFormSchema),
    defaultValues: EMPTY_FORM,
  });

  function openAdd() {
    setEditingId(null);
    reset(EMPTY_FORM);
    setShowForm(true);
  }

  function openEdit(config: AiModelConfig) {
    setEditingId(config.id);
    reset({
      label: config.label,
      provider: config.provider,
      model: config.model,
      baseUrl: config.baseUrl,
      apiKey: "",
    });
    setShowForm(true);
  }

  async function onSubmit(values: AiConfigFormInput) {
    if (editingId) {
      await updateConfig.mutateAsync({ id: editingId, ...values });
    } else {
      await createConfig.mutateAsync(values);
    }
    setShowForm(false);
  }

  async function toggleReveal(config: AiModelConfig) {
    if (revealed[config.id]) {
      setRevealed((prev) => {
        const next = { ...prev };
        delete next[config.id];
        return next;
      });
      return;
    }
    const { apiKey } = await revealKey.mutateAsync(config.id);
    setRevealed((prev) => ({ ...prev, [config.id]: apiKey }));
  }

  function confirmDelete() {
    if (!configToDelete) return;
    deleteConfig.mutate(configToDelete.id, {
      onSuccess: () => setConfigToDelete(null),
    });
  }

  const saving = createConfig.isPending || updateConfig.isPending;

  return (
    <div>
      <div className="flex items-center justify-between mb-3.5">
        <div className="text-[15px] font-bold">AI model configurations</div>
        <Button onClick={openAdd} className="h-9 rounded-lg px-3.5 text-[13px]">
          + Add configuration
        </Button>
      </div>

      {showForm && (
        <Card className="mb-4 rounded-[12px] bg-[#fafafa] p-5">
          <form onSubmit={handleSubmit(onSubmit)}>
            <div className="text-[13px] font-bold mb-3.5">
              {editingId ? "Edit configuration" : "New configuration"}
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-3.5">
              <Field className="gap-1.5">
                <FieldLabel
                  htmlFor="ai-config-label"
                  className="text-xs font-semibold"
                >
                  Label
                </FieldLabel>
                <Controller
                  control={control}
                  name="label"
                  render={({ field }) => (
                    <Input
                      id="ai-config-label"
                      placeholder="e.g. GPT-4.1"
                      {...field}
                    />
                  )}
                />
              </Field>
              <Field className="gap-1.5">
                <FieldLabel
                  htmlFor="ai-config-provider"
                  className="text-xs font-semibold"
                >
                  Provider
                </FieldLabel>
                <Controller
                  control={control}
                  name="provider"
                  render={({ field }) => (
                    <Select
                      value={field.value}
                      onValueChange={(v) => v && field.onChange(v)}
                    >
                      <SelectTrigger id="ai-config-provider" className="w-full">
                        <SelectValue>
                          {() =>
                            PROVIDERS.find((p) => p.value === field.value)
                              ?.label ?? field.value
                          }
                        </SelectValue>
                      </SelectTrigger>
                      <SelectContent>
                        {PROVIDERS.map((p) => (
                          <SelectItem key={p.value} value={p.value}>
                            {p.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </Field>
              <Field className="gap-1.5">
                <FieldLabel
                  htmlFor="ai-config-model"
                  className="text-xs font-semibold"
                >
                  Model name
                </FieldLabel>
                <Controller
                  control={control}
                  name="model"
                  render={({ field }) => (
                    <Input
                      id="ai-config-model"
                      placeholder="e.g. gpt-4.1"
                      {...field}
                    />
                  )}
                />
              </Field>
              <Field className="gap-1.5">
                <FieldLabel
                  htmlFor="ai-config-base-url"
                  className="text-xs font-semibold"
                >
                  Base URL
                </FieldLabel>
                <Controller
                  control={control}
                  name="baseUrl"
                  render={({ field }) => (
                    <Input
                      id="ai-config-base-url"
                      placeholder="https://api.example.com/v1"
                      {...field}
                    />
                  )}
                />
              </Field>
              <Field className="gap-1.5 sm:col-span-2">
                <FieldLabel
                  htmlFor="ai-config-api-key"
                  className="text-xs font-semibold"
                >
                  API key
                </FieldLabel>
                <Controller
                  control={control}
                  name="apiKey"
                  render={({ field }) => (
                    <Input
                      id="ai-config-api-key"
                      placeholder="sk-..."
                      type="password"
                      {...field}
                    />
                  )}
                />
              </Field>
            </div>
            <div className="flex justify-end gap-2.5">
              <Button
                type="button"
                variant="outline"
                onClick={() => setShowForm(false)}
              >
                Cancel
              </Button>
              <Button type="submit" disabled={saving}>
                {saving
                  ? "Saving…"
                  : editingId
                    ? "Save changes"
                    : "Add configuration"}
              </Button>
            </div>
          </form>
        </Card>
      )}

      <Card className="mb-9 rounded-[12px] p-0 overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Label</TableHead>
              <TableHead>Provider</TableHead>
              <TableHead>Model</TableHead>
              <TableHead>Base URL</TableHead>
              <TableHead>API key</TableHead>
              <TableHead className="text-right"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {configs.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="py-10">
                  <Empty className="min-h-0 border-0 p-0">
                    <EmptyDescription>
                      No model configurations yet.
                    </EmptyDescription>
                  </Empty>
                </TableCell>
              </TableRow>
            ) : (
              configs.map((config) => (
                <TableRow key={config.id}>
                  <TableCell className="font-semibold text-[13.5px]">
                    {config.label}
                  </TableCell>
                  <TableCell className="text-sm">
                    {PROVIDERS.find((p) => p.value === config.provider)
                      ?.label ?? config.provider}
                  </TableCell>
                  <TableCell className="text-sm font-mono">
                    {config.model}
                  </TableCell>
                  <TableCell className="text-xs text-muted-foreground truncate max-w-[180px]">
                    {config.baseUrl}
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-mono">
                        {revealed[config.id] ?? config.maskedApiKey}
                      </span>
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => toggleReveal(config)}
                        className="h-6 rounded-md px-1 text-[11.5px] font-semibold text-blue-600 hover:bg-transparent hover:text-blue-700 hover:underline"
                      >
                        {revealed[config.id] ? "Hide" : "Show"}
                      </Button>
                    </div>
                  </TableCell>
                  <TableCell className="text-right">
                    <ButtonGroup
                      className="ml-auto"
                      aria-label={`Actions for ${config.label}`}
                    >
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => openEdit(config)}
                      >
                        Edit
                      </Button>
                      <Button
                        type="button"
                        variant="destructive"
                        size="sm"
                        onClick={() => setConfigToDelete(config)}
                      >
                        Delete
                      </Button>
                    </ButtonGroup>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </Card>

      <AlertDialog
        open={Boolean(configToDelete)}
        onOpenChange={(open) => {
          if (!open && !deleteConfig.isPending) setConfigToDelete(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogMedia>
              <CircleAlertIcon />
            </AlertDialogMedia>
            <AlertDialogTitle>Delete configuration?</AlertDialogTitle>
            <AlertDialogDescription>
              {configToDelete
                ? `This permanently removes ${configToDelete.label} and unassigns it from any routed tasks.`
                : "This permanently removes the selected configuration."}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleteConfig.isPending}>
              Cancel
            </AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              disabled={deleteConfig.isPending}
              onClick={confirmDelete}
            >
              {deleteConfig.isPending ? "Deleting…" : "Delete configuration"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <div className="text-[15px] font-bold mb-3.5">Task routing</div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {TASK_DEFS.map((task) => {
          const currentRouting = routing.find((r) => r.task === task.key);
          return (
            <Card key={task.key} className="rounded-[12px] p-4.5">
              <Field className="gap-1">
                <FieldLabel
                  htmlFor={`task-routing-${task.key}`}
                  className="text-sm font-bold"
                >
                  {task.label}
                </FieldLabel>
                <FieldDescription className="mb-2.5 text-[12.5px]">
                  {task.key === "specGeneration"
                    ? `${task.desc} Falls back to whatever is routed to Code generation when left unassigned.`
                    : task.desc}
                </FieldDescription>
                <Select
                  value={currentRouting?.aiModelConfigId ?? "__unassigned__"}
                  onValueChange={(v) =>
                    updateRouting.mutate({
                      task: task.key,
                      aiModelConfigId:
                        v === "__unassigned__" ? null : (v ?? null),
                    })
                  }
                >
                  <SelectTrigger
                    id={`task-routing-${task.key}`}
                    className="w-full"
                  >
                    <SelectValue>
                      {() => {
                        const assigned = configs.find(
                          (c) => c.id === currentRouting?.aiModelConfigId,
                        );
                        return assigned?.label ?? "Unassigned";
                      }}
                    </SelectValue>
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="__unassigned__">Unassigned</SelectItem>
                    {configs.map((config) => (
                      <SelectItem key={config.id} value={config.id}>
                        {config.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </Field>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
