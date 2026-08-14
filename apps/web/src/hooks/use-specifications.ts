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

export function useSpecificationDocuments(id: string | undefined) {
  return useQuery({
    queryKey: ["specifications", id, "documents"],
    queryFn: () => apiClient.get<string[]>(`/api/specifications/${id}/documents`),
    enabled: Boolean(id),
  });
}

export function useSpecificationDocument(id: string | undefined, path: string | null) {
  return useQuery({
    queryKey: ["specifications", id, "documents", path],
    queryFn: () =>
      apiClient.get<{ path: string; content: string }>(
        `/api/specifications/${id}/documents/content?path=${encodeURIComponent(path!)}`,
      ),
    enabled: Boolean(id) && Boolean(path),
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

export function useDeleteSpecification() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/specifications/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["specifications"] });
    },
  });
}
