import { useQueries, useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { GenerationDetail, SpecificationDetail, SpecificationSummary } from "@/lib/types";

export function useGeneratedProjects() {
  return useQuery({
    queryKey: ["generatedProjects"],
    queryFn: () => apiClient.get<SpecificationSummary[]>("/api/generated-projects"),
  });
}

/**
 * Fans out one detail fetch per project to get each one's `generations`
 * array (the summary list only carries a count) -- mirrors the N+1 the
 * Next.js pages did via `Promise.all`, but as concurrent queries.
 */
export function useGeneratedProjectsWithVersions(projects: SpecificationSummary[]) {
  return useQueries({
    queries: projects.map((spec) => ({
      queryKey: ["specifications", spec.id],
      queryFn: () => apiClient.get<SpecificationDetail>(`/api/specifications/${spec.id}`),
    })),
    combine: (results) => ({
      data: projects.map((spec, i) => ({ spec, generations: results[i].data?.generations ?? [] })),
      isPending: results.some((r) => r.isPending),
    }),
  });
}

export function useGeneration(id: string | undefined) {
  return useQuery({
    queryKey: ["generations", id],
    queryFn: () => apiClient.get<GenerationDetail>(`/api/generations/${id}`),
    enabled: Boolean(id),
  });
}

export function useGenerationFile(id: string | undefined, path: string | null) {
  return useQuery({
    queryKey: ["generations", id, "files", path],
    queryFn: () =>
      apiClient.get<{ path: string; content: string }>(
        `/api/generations/${id}/files?path=${encodeURIComponent(path!)}`,
      ),
    enabled: Boolean(id) && Boolean(path),
  });
}
