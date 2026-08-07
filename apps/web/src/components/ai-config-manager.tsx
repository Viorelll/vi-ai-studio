import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
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

const EMPTY_FORM: AiConfigFormInput = { label: "", provider: "openAi", model: "", baseUrl: "", apiKey: "" };

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
    reset({ label: config.label, provider: config.provider, model: config.model, baseUrl: config.baseUrl, apiKey: "" });
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

  const saving = createConfig.isPending || updateConfig.isPending;

  return (
    <div>
      <div className="flex items-center justify-between mb-3.5">
        <div className="text-[15px] font-bold">AI model configurations</div>
        <Button onClick={openAdd}>+ Add configuration</Button>
      </div>

      {showForm && (
        <Card className="p-5 mb-4 bg-muted/30">
          <form onSubmit={handleSubmit(onSubmit)}>
            <div className="text-[13px] font-bold mb-3.5">{editingId ? "Edit configuration" : "New configuration"}</div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-3.5">
              <div>
                <Label className="text-xs font-semibold mb-1.5">Label</Label>
                <Controller
                  control={control}
                  name="label"
                  render={({ field }) => <Input placeholder="e.g. GPT-4.1" {...field} />}
                />
              </div>
              <div>
                <Label className="text-xs font-semibold mb-1.5">Provider</Label>
                <Controller
                  control={control}
                  name="provider"
                  render={({ field }) => (
                    <Select value={field.value} onValueChange={(v) => v && field.onChange(v)}>
                      <SelectTrigger className="w-full">
                        <SelectValue>{() => PROVIDERS.find((p) => p.value === field.value)?.label ?? field.value}</SelectValue>
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
              </div>
              <div>
                <Label className="text-xs font-semibold mb-1.5">Model name</Label>
                <Controller
                  control={control}
                  name="model"
                  render={({ field }) => <Input placeholder="e.g. gpt-4.1" {...field} />}
                />
              </div>
              <div>
                <Label className="text-xs font-semibold mb-1.5">Base URL</Label>
                <Controller
                  control={control}
                  name="baseUrl"
                  render={({ field }) => <Input placeholder="https://api.example.com/v1" {...field} />}
                />
              </div>
              <div className="sm:col-span-2">
                <Label className="text-xs font-semibold mb-1.5">API key</Label>
                <Controller
                  control={control}
                  name="apiKey"
                  render={({ field }) => <Input placeholder="sk-..." type="password" {...field} />}
                />
              </div>
            </div>
            <div className="flex justify-end gap-2.5">
              <Button type="button" variant="outline" onClick={() => setShowForm(false)}>
                Cancel
              </Button>
              <Button type="submit" disabled={saving}>
                {saving ? "Saving…" : editingId ? "Save changes" : "Add configuration"}
              </Button>
            </div>
          </form>
        </Card>
      )}

      <Card className="p-0 overflow-hidden mb-9">
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
                <TableCell colSpan={6} className="text-center text-muted-foreground py-10">
                  No model configurations yet.
                </TableCell>
              </TableRow>
            ) : (
              configs.map((config) => (
                <TableRow key={config.id}>
                  <TableCell className="font-semibold text-[13.5px]">{config.label}</TableCell>
                  <TableCell className="text-sm">{PROVIDERS.find((p) => p.value === config.provider)?.label ?? config.provider}</TableCell>
                  <TableCell className="text-sm font-mono">{config.model}</TableCell>
                  <TableCell className="text-xs text-muted-foreground truncate max-w-[180px]">{config.baseUrl}</TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-mono">{revealed[config.id] ?? config.maskedApiKey}</span>
                      <button
                        type="button"
                        onClick={() => toggleReveal(config)}
                        className="text-[11.5px] font-semibold text-blue-600 hover:underline"
                      >
                        {revealed[config.id] ? "Hide" : "Show"}
                      </button>
                    </div>
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-1.5">
                      <Button variant="outline" size="sm" onClick={() => openEdit(config)}>
                        Edit
                      </Button>
                      <Button variant="destructive" size="sm" onClick={() => deleteConfig.mutate(config.id)}>
                        Delete
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </Card>

      <div className="text-[15px] font-bold mb-3.5">Task routing</div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {TASK_DEFS.map((task) => {
          const currentRouting = routing.find((r) => r.task === task.key);
          return (
            <Card key={task.key} className="p-4.5">
              <div className="text-sm font-bold mb-1">{task.label}</div>
              <p className="text-[12.5px] text-muted-foreground mb-3.5">{task.desc}</p>
              <Select
                value={currentRouting?.aiModelConfigId ?? "__unassigned__"}
                onValueChange={(v) =>
                  updateRouting.mutate({ task: task.key, aiModelConfigId: v === "__unassigned__" ? null : (v ?? null) })
                }
              >
                <SelectTrigger className="w-full">
                  <SelectValue>
                    {() => {
                      const assigned = configs.find((c) => c.id === currentRouting?.aiModelConfigId);
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
            </Card>
          );
        })}
      </div>
    </div>
  );
}
