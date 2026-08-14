import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { ChipGroup, IntakeSheet, InterviewAnswer, InterviewRound } from "@/lib/types";

export function useChipGroups(specId: string | undefined) {
  return useQuery({
    queryKey: ["specifications", "intake", "chip-groups"],
    queryFn: () => apiClient.get<ChipGroup[]>(`/api/specifications/${specId}/intake/chip-groups`),
    enabled: Boolean(specId),
    staleTime: Infinity,
  });
}

export function useIntakeSheet(specId: string | undefined) {
  return useQuery({
    queryKey: ["specifications", specId, "intake"],
    queryFn: () => apiClient.get<IntakeSheet | null>(`/api/specifications/${specId}/intake/`),
    enabled: Boolean(specId),
  });
}

export function useSaveIntakeChips(specId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (sheet: Omit<IntakeSheet, "impliedDecisions" | "conflictsResolved" | "stillUnknown" | "completedAt" | "interviewCompletedAt">) =>
      apiClient.put<IntakeSheet>(`/api/specifications/${specId}/intake/chips`, sheet),
    onSuccess: (data) => {
      queryClient.setQueryData(["specifications", specId, "intake"], data);
    },
  });
}

export function useInterviewRounds(specId: string | undefined) {
  return useQuery({
    queryKey: ["specifications", "intake", "interview-rounds"],
    queryFn: () => apiClient.get<InterviewRound[]>(`/api/specifications/${specId}/intake/interview-rounds`),
    enabled: Boolean(specId),
    staleTime: Infinity,
  });
}

export function useInterviewAnswers(specId: string | undefined) {
  return useQuery({
    queryKey: ["specifications", specId, "intake", "interview-answers"],
    queryFn: () => apiClient.get<InterviewAnswer[]>(`/api/specifications/${specId}/intake/interview-answers`),
    enabled: Boolean(specId),
  });
}

export function useSaveInterviewRound(specId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      roundIndex,
      answers,
    }: {
      roundIndex: number;
      answers: { questionIndex: number; questionText: string; defaultHint: string; answerText: string | null }[];
    }) => apiClient.put<InterviewAnswer[]>(`/api/specifications/${specId}/intake/interview/${roundIndex}`, { answers }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["specifications", specId, "intake", "interview-answers"] });
    },
  });
}

export function useExpandInterviewAnswer(specId: string) {
  return useMutation({
    mutationFn: ({ questionText, answerText }: { questionText: string; answerText: string }) =>
      apiClient.post<{ expandedText: string }>(`/api/specifications/${specId}/intake/interview/expand`, {
        questionText,
        answerText,
      }),
  });
}

export function useCompleteInterview(specId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => apiClient.post<IntakeSheet>(`/api/specifications/${specId}/intake/complete`),
    onSuccess: (data) => {
      queryClient.setQueryData(["specifications", specId, "intake"], data);
    },
  });
}
