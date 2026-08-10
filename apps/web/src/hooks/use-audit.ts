import { useMutation, useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { AiCallLogDetail, AiCallLogRollup, AiCallLogSummary } from "@/lib/types";

/**
 * Which activity's spend to report. Specification authoring and AI Build are
 * billed to different work and are never summed together -- see AiCallLogScope
 * on the Api side.
 */
export type AuditScope = "specification" | "build";

export function useAuditRollups(scope: AuditScope) {
  return useQuery({
    queryKey: ["auditRollups", scope],
    queryFn: () =>
      apiClient.get<AiCallLogRollup[]>(`/api/admin/audit/specifications?scope=${scope}`),
  });
}

export function useAuditLogs(specId: string | undefined, scope: AuditScope) {
  return useQuery({
    queryKey: ["auditLogs", specId, scope],
    queryFn: () =>
      apiClient.get<AiCallLogSummary[]>(
        `/api/admin/audit/specifications/${specId}?scope=${scope}`,
      ),
    enabled: Boolean(specId),
  });
}

export function useAuditLogDetail() {
  return useMutation({
    mutationFn: (logId: string) => apiClient.get<AiCallLogDetail>(`/api/admin/audit/logs/${logId}`),
  });
}
