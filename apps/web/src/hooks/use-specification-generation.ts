import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient, API_BASE_URL, getAuthHeaders } from "@/lib/api-client";
import { clearAuth } from "@/api/auth-token";
import type { BuildEvent, SpecificationGenerationRun, ValidationIssue } from "@/lib/types";

export function useGenerationRuns(specId: string | undefined) {
  return useQuery({
    queryKey: ["specifications", specId, "generation-runs"],
    queryFn: () => apiClient.get<SpecificationGenerationRun[]>(`/api/specifications/${specId}/generation-runs`),
    enabled: Boolean(specId),
  });
}

export function useGenerationRun(specId: string | undefined, runId: string | null) {
  return useQuery({
    queryKey: ["specifications", specId, "generation-runs", runId],
    queryFn: () => apiClient.get<SpecificationGenerationRun>(`/api/specifications/${specId}/generation-runs/${runId}`),
    enabled: Boolean(specId) && Boolean(runId),
  });
}

export function useStartGenerationRun(specId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => apiClient.post<SpecificationGenerationRun>(`/api/specifications/${specId}/generation-runs`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["specifications", specId, "generation-runs"] });
    },
  });
}

export function useValidationIssues(specId: string | undefined) {
  return useQuery({
    queryKey: ["specifications", specId, "validation-issues"],
    queryFn: () => apiClient.get<ValidationIssue[]>(`/api/specifications/${specId}/validation-issues`),
    enabled: Boolean(specId),
  });
}

/**
 * Raw SSE consumption for one generation run's live progress -- copies
 * build-progress.tsx's fetch + ReadableStream + SSE-frame-splitting loop.
 * Calls onEvent for every batch update so the caller can refetch the
 * document list live; stops itself once a Done event arrives.
 */
export function useSpecificationGenerationStream(
  specId: string | undefined,
  runId: string | null,
  onEvent: (event: BuildEvent) => void,
) {
  const [done, setDone] = useState(false);
  const onEventRef = useRef(onEvent);
  onEventRef.current = onEvent;

  useEffect(() => {
    if (!specId || !runId) return;
    setDone(false);
    const controller = new AbortController();

    const readStream = async () => {
      try {
        const response = await fetch(
          `${API_BASE_URL}/api/specifications/${specId}/generation-runs/${runId}/stream`,
          { headers: getAuthHeaders(), signal: controller.signal },
        );

        if (response.status === 401) {
          clearAuth();
          return;
        }
        if (!response.ok || !response.body) {
          throw new Error(`Generation stream failed with ${response.status}.`);
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = "";

        while (true) {
          const { done: streamDone, value } = await reader.read();
          if (streamDone) break;

          buffer += decoder.decode(value, { stream: true });
          const events = buffer.split("\n\n");
          buffer = events.pop() ?? "";

          for (const event of events) {
            const dataLine = event.split("\n").find((line) => line.startsWith("data:"));
            if (!dataLine) continue;

            const data: BuildEvent = JSON.parse(dataLine.slice(5).trim());
            onEventRef.current(data);
            if (data.done) {
              setDone(true);
              controller.abort();
              return;
            }
          }
        }
      } catch (error) {
        if (error instanceof DOMException && error.name === "AbortError") return;
      }
    };

    void readStream();
    return () => controller.abort();
  }, [specId, runId]);

  return { done };
}
