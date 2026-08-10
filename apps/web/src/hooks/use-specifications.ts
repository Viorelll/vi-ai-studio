import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { SpecificationDetail, SpecificationSummary, TechStack } from "@/lib/types";

export function useSpecifications() {
  return useQuery({
    queryKey: ["specifications"],
    queryFn: () => apiClient.get<SpecificationSummary[]>("/api/specifications"),
  });
}

export function useSpecification(id: string | undefined) {
  return useQuery({
    queryKey: ["specifications", id],
    queryFn: () => apiClient.get<SpecificationDetail>(`/api/specifications/${id}`),
    enabled: Boolean(id),
  });
}

export function useCreateSpecification() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: { name: string; summary: string; owner?: string }) =>
      apiClient.post<SpecificationDetail>("/api/specifications", input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["specifications"] });
    },
  });
}

export function useUpdateSpecificationBasics(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: { name?: string; summary?: string; audience?: string; stack?: TechStack }) =>
      apiClient.patch<SpecificationDetail>(`/api/specifications/${id}`, input),
    onSuccess: (data) => {
      queryClient.setQueryData(["specifications", id], data);
      queryClient.invalidateQueries({ queryKey: ["specifications"] });
    },
  });
}

export function useSaveSpecificationPhase(specId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: { phaseIndex: number; checkedItems: string[]; selectedKeywords: string[] }) =>
      apiClient.put(`/api/specifications/${specId}/phases/${input.phaseIndex}`, {
        checkedItems: input.checkedItems,
        selectedKeywords: input.selectedKeywords,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["specifications", specId] });
    },
  });
}

export function useGeneratePhaseText(specId: string) {
  return useMutation({
    mutationFn: (phaseIndex: number) =>
      apiClient.post<{ generatedText: string | null }>(`/api/specifications/${specId}/phases/${phaseIndex}/generate`),
  });
}

export function useDeleteSpecification() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/specifications/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["specifications"] });
    },
  });
}

export function useFinalizeSpecification(specId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => apiClient.post<SpecificationDetail>(`/api/specifications/${specId}/finalize`),
    onSuccess: (data) => {
      queryClient.setQueryData(["specifications", specId], data);
      queryClient.invalidateQueries({ queryKey: ["specifications"] });
    },
  });
}
