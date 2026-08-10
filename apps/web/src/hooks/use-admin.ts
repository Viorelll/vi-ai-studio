import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { AiModelConfig, TaskRouting } from "@/lib/types";

export function useServiceStatuses(
  services: readonly { name: string; endpoint: string; mode?: RequestMode }[],
) {
  const [statuses, setStatuses] = useState<Record<string, boolean>>({});

  useEffect(() => {
    let disposed = false;

    const checkServices = async () => {
      const results = await Promise.all(
        services.map(async (service) => {
          try {
            const response = await fetch(service.endpoint, {
              cache: "no-store",
              mode: service.mode ?? "cors",
              signal: AbortSignal.timeout(3000),
            });
            return [
              service.name,
              response.ok || response.type === "opaque",
            ] as const;
          } catch {
            return [service.name, false] as const;
          }
        }),
      );

      if (!disposed) {
        setStatuses(Object.fromEntries(results));
      }
    };

    void checkServices();

    return () => {
      disposed = true;
    };
  }, [services]);

  return statuses;
}

export function useAiConfigs() {
  return useQuery({
    queryKey: ["aiConfigs"],
    queryFn: () => apiClient.get<AiModelConfig[]>("/api/admin/ai-configs"),
  });
}

export function useTaskRouting() {
  return useQuery({
    queryKey: ["taskRouting"],
    queryFn: () => apiClient.get<TaskRouting[]>("/api/admin/task-routing"),
  });
}

export interface AiConfigFormInput {
  label: string;
  provider: string;
  model: string;
  baseUrl: string;
  apiKey: string;
}

export function useCreateAiConfig() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AiConfigFormInput) =>
      apiClient.post<AiModelConfig>("/api/admin/ai-configs", input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["aiConfigs"] }),
  });
}

export function useUpdateAiConfig() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...input }: AiConfigFormInput & { id: string }) =>
      apiClient.put<AiModelConfig>(`/api/admin/ai-configs/${id}`, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["aiConfigs"] }),
  });
}

export function useDeleteAiConfig() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/admin/ai-configs/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["aiConfigs"] });
      queryClient.invalidateQueries({ queryKey: ["taskRouting"] });
    },
  });
}

export function useRevealAiConfigKey() {
  return useMutation({
    mutationFn: (id: string) =>
      apiClient.get<{ apiKey: string }>(`/api/admin/ai-configs/${id}/reveal`),
  });
}

export function useUpdateTaskRouting() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      task,
      aiModelConfigId,
    }: {
      task: string;
      aiModelConfigId: string | null;
    }) =>
      apiClient.put<TaskRouting>(`/api/admin/task-routing/${task}`, {
        aiModelConfigId,
      }),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["taskRouting"] }),
  });
}
