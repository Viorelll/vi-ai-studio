import { useMutation, useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { AiCallLogDetail, AiCallLogRollup, AiCallLogSummary } from "@/lib/types";

export function useAuditRollups() {
  return useQuery({
    queryKey: ["auditRollups"],
    queryFn: () => apiClient.get<AiCallLogRollup[]>("/api/admin/audit/specifications"),
  });
}

export function useAuditLogs(specId: string | undefined) {
  return useQuery({
    queryKey: ["auditLogs", specId],
    queryFn: () => apiClient.get<AiCallLogSummary[]>(`/api/admin/audit/specifications/${specId}`),
    enabled: Boolean(specId),
  });
}

export function useAuditLogDetail() {
  return useMutation({
    mutationFn: (logId: string) => apiClient.get<AiCallLogDetail>(`/api/admin/audit/logs/${logId}`),
  });
}
