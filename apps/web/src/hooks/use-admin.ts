import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { AiModelConfig, ServiceHealth, TaskRouting } from "@/lib/types";

export function useServices() {
  return useQuery({
    queryKey: ["services"],
    queryFn: () => apiClient.get<ServiceHealth[]>("/api/admin/services"),
  });
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
    mutationFn: (input: AiConfigFormInput) => apiClient.post<AiModelConfig>("/api/admin/ai-configs", input),
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
    mutationFn: (id: string) => apiClient.get<{ apiKey: string }>(`/api/admin/ai-configs/${id}/reveal`),
  });
}

export function useUpdateTaskRouting() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ task, aiModelConfigId }: { task: string; aiModelConfigId: string | null }) =>
      apiClient.put<TaskRouting>(`/api/admin/task-routing/${task}`, { aiModelConfigId }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["taskRouting"] }),
  });
}
