import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { GenerationSummary } from "@/lib/types";

export function useStartBuild(specId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (aiModelConfigId: string | null) =>
      apiClient.post<GenerationSummary>(`/api/specifications/${specId}/builds`, { aiModelConfigId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["specifications", specId] });
      queryClient.invalidateQueries({ queryKey: ["specifications"] });
    },
  });
}
