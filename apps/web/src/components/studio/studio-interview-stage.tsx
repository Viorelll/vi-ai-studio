import { useEffect, useState } from "react";
import { SparklesIcon } from "lucide-react";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Spinner } from "@/components/ui/spinner";
import {
  useCompleteInterview,
  useExpandInterviewAnswer,
  useInterviewAnswers,
  useInterviewRounds,
  useSaveInterviewRound,
} from "@/hooks/use-specification-intake";
import type { InterviewAnswer } from "@/lib/types";
import { ApiError } from "@/lib/api-client";

export function StudioInterviewStage({ specId, onCompleted }: { specId: string; onCompleted: () => void }) {
  const roundsQuery = useInterviewRounds(specId);
  const answersQuery = useInterviewAnswers(specId);
  const saveRound = useSaveInterviewRound(specId);
  const expandAnswer = useExpandInterviewAnswer(specId);
  const completeInterview = useCompleteInterview(specId);

  const [activeRound, setActiveRound] = useState(0);
  const [drafts, setDrafts] = useState<Record<number, string>>({});
  const [expandingIndex, setExpandingIndex] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  const round = roundsQuery.data?.[activeRound];

  useEffect(() => {
    if (!round || !answersQuery.data) return;
    const next: Record<number, string> = {};
    for (const question of round.questions) {
      const existing = answersQuery.data.find(
        (a: InterviewAnswer) => a.roundIndex === round.round && a.questionIndex === question.order,
      );
      next[question.order] = existing && !existing.usedDefault ? (existing.answerText ?? "") : "";
    }
    setDrafts(next);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [round?.round, answersQuery.data]);

  if (roundsQuery.isPending || answersQuery.isPending) {
    return (
      <div className="flex items-center gap-2 p-10 text-sm text-muted-foreground">
        <Spinner />
        Loading…
      </div>
    );
  }
  if (roundsQuery.isError || !roundsQuery.data || !round) {
    return <div className="p-10 text-sm text-destructive">Couldn't load the interview.</div>;
  }

  const isLastRound = activeRound === roundsQuery.data.length - 1;

  async function tighten(questionOrder: number, questionText: string) {
    const current = drafts[questionOrder]?.trim();
    if (!current) return;
    setExpandingIndex(questionOrder);
    try {
      const result = await expandAnswer.mutateAsync({ questionText, answerText: current });
      setDrafts((prev) => ({ ...prev, [questionOrder]: result.expandedText }));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Couldn't tighten that answer.");
    } finally {
      setExpandingIndex(null);
    }
  }

  async function saveCurrentRound() {
    if (!round) return;
    await saveRound.mutateAsync({
      roundIndex: round.round,
      answers: round.questions.map((q) => ({
        questionIndex: q.order,
        questionText: q.prompt,
        defaultHint: q.defaultHint,
        answerText: drafts[q.order]?.trim() ? drafts[q.order] : null,
      })),
    });
  }

  async function handleBack() {
    setError(null);
    if (activeRound === 0) return;
    await saveCurrentRound();
    setActiveRound((r) => r - 1);
  }

  async function handleNext() {
    setError(null);
    try {
      await saveCurrentRound();
      if (isLastRound) {
        await completeInterview.mutateAsync();
        onCompleted();
      } else {
        setActiveRound((r) => r + 1);
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Couldn't save this round.");
    }
  }

  const busy = saveRound.isPending || completeInterview.isPending;

  return (
    <div className="flex flex-col gap-5">
      <div>
        <div className="text-xs font-semibold text-[#71717a] uppercase tracking-wide mb-1">
          Round {activeRound + 1} of {roundsQuery.data.length}
        </div>
        <h2 className="text-xl font-bold tracking-tight mb-1.5">{round.title}</h2>
        <p className="text-sm text-muted-foreground">
          Answer in your own words, or leave a field blank to use the shown default.
        </p>
      </div>

      <div className="flex gap-1.5">
        {roundsQuery.data.map((r, i) => (
          <div
            key={r.round}
            className={`h-1 flex-1 rounded-full ${i < activeRound ? "bg-[var(--brand)]" : i === activeRound ? "bg-[#93c5fd]" : "bg-[#e4e4e7]"}`}
          />
        ))}
      </div>

      <Card className="rounded-[12px] p-6 gap-5">
        {round.questions.map((question) => (
          <div key={question.order} className="flex flex-col gap-1.5">
            <div className="flex items-center justify-between gap-2">
              <Label className="text-[13px] font-semibold">{question.prompt}</Label>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => tighten(question.order, question.prompt)}
                disabled={expandingIndex === question.order || !drafts[question.order]?.trim()}
              >
                <SparklesIcon className="size-3.5" />
                {expandingIndex === question.order ? "Tightening…" : "Tighten"}
              </Button>
            </div>
            <Textarea
              rows={2}
              placeholder={`Default: ${question.defaultHint}`}
              value={drafts[question.order] ?? ""}
              onChange={(e) => setDrafts((prev) => ({ ...prev, [question.order]: e.target.value }))}
            />
          </div>
        ))}
      </Card>

      {error && <p className="text-[12.5px] text-destructive">{error}</p>}

      <div className="flex justify-between">
        <Button variant="outline" onClick={handleBack} disabled={activeRound === 0 || busy}>
          Back
        </Button>
        <Button onClick={handleNext} disabled={busy}>
          {busy ? "Saving…" : isLastRound ? "Finish interview" : "Next round"}
        </Button>
      </div>
    </div>
  );
}
