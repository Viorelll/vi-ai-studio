import { useState } from "react";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Card } from "@/components/ui/card";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog";
import { useAuditLogDetail } from "@/hooks/use-audit";
import { formatDateTime } from "@/lib/format";
import type { AiCallLogDetail, AiCallLogSummary } from "@/lib/types";

const TASK_LABELS: Record<string, string> = {
  specGeneration: "Spec generation",
  codeGeneration: "Code generation",
  imageGeneration: "Image generation",
  soundGeneration: "Sound generation",
  transcribing: "Transcribing",
};

export function AuditLogTable({ logs }: { logs: AiCallLogSummary[] }) {
  const [selected, setSelected] = useState<AiCallLogDetail | null>(null);
  const [open, setOpen] = useState(false);
  const logDetail = useAuditLogDetail();

  async function openLog(id: string) {
    const detail = await logDetail.mutateAsync(id);
    setSelected(detail);
    setOpen(true);
  }

  return (
    <>
      <Card className="p-0 overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Time</TableHead>
              <TableHead>Model</TableHead>
              <TableHead>Task</TableHead>
              <TableHead>Requests</TableHead>
              <TableHead>Tokens (in/out)</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {logs.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center text-muted-foreground py-10">
                  No log entries yet.
                </TableCell>
              </TableRow>
            ) : (
              logs.map((log) => (
                <TableRow key={log.id} className="cursor-pointer" onClick={() => openLog(log.id)}>
                  <TableCell className="text-xs text-muted-foreground">{formatDateTime(log.created)}</TableCell>
                  <TableCell className="text-sm font-semibold">{log.model}</TableCell>
                  <TableCell className="text-sm">{TASK_LABELS[log.task] ?? log.task}</TableCell>
                  <TableCell className="text-sm">{log.requests}</TableCell>
                  <TableCell className="text-xs font-mono">
                    {log.tokensIn.toLocaleString("en-US")} / {log.tokensOut.toLocaleString("en-US")}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </Card>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-w-[640px] max-h-[80vh] overflow-y-auto">
          {selected && (
            <>
              <DialogHeader>
                <DialogTitle>
                  {selected.model} · {TASK_LABELS[selected.task] ?? selected.task}
                </DialogTitle>
                <DialogDescription>
                  {formatDateTime(selected.created)} · {selected.requests} requests ·{" "}
                  {(selected.tokensIn + selected.tokensOut).toLocaleString("en-US")} tokens (
                  {selected.tokensIn.toLocaleString("en-US")} in / {selected.tokensOut.toLocaleString("en-US")} out)
                </DialogDescription>
              </DialogHeader>
              <div>
                <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-1.5">Input prompt</div>
                <div className="text-[13.5px] leading-relaxed bg-muted/40 border rounded-lg p-3.5 mb-4 whitespace-pre-wrap">
                  {selected.prompt}
                </div>
                <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-1.5">Result</div>
                <div className="text-[13.5px] leading-relaxed bg-muted/40 border rounded-lg p-3.5 whitespace-pre-wrap">
                  {selected.result}
                </div>
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>
    </>
  );
}
