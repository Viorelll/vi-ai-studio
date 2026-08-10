import { useState } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Card } from "@/components/ui/card";
import { Empty, EmptyDescription } from "@/components/ui/empty";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
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
                <TableCell colSpan={5} className="py-10">
                  <Empty className="min-h-0 border-0 p-0">
                    <EmptyDescription>No log entries yet.</EmptyDescription>
                  </Empty>
                </TableCell>
              </TableRow>
            ) : (
              logs.map((log) => (
                <TableRow
                  key={log.id}
                  className="cursor-pointer"
                  onClick={() => openLog(log.id)}
                >
                  <TableCell className="text-xs text-muted-foreground">
                    {formatDateTime(log.created)}
                  </TableCell>
                  <TableCell className="text-sm font-semibold">
                    {log.model}
                  </TableCell>
                  <TableCell className="text-sm">
                    {TASK_LABELS[log.task] ?? log.task}
                  </TableCell>
                  <TableCell className="text-sm">{log.requests}</TableCell>
                  <TableCell className="text-xs font-mono">
                    {log.tokensIn.toLocaleString("en-US")} /{" "}
                    {log.tokensOut.toLocaleString("en-US")}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </Card>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-h-[80vh] max-w-[640px] overflow-y-auto rounded-[14px]">
          {selected && (
            <>
              <DialogHeader>
                <DialogTitle>
                  {selected.model} ·{" "}
                  {TASK_LABELS[selected.task] ?? selected.task}
                </DialogTitle>
                <DialogDescription>
                  {formatDateTime(selected.created)} · {selected.requests}{" "}
                  requests ·{" "}
                  {(selected.tokensIn + selected.tokensOut).toLocaleString(
                    "en-US",
                  )}{" "}
                  tokens ({selected.tokensIn.toLocaleString("en-US")} in /{" "}
                  {selected.tokensOut.toLocaleString("en-US")} out)
                </DialogDescription>
              </DialogHeader>
              <div>
                <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-1.5">
                  Input prompt
                </div>
                <Card className="mb-4.5 gap-0 rounded-[10px] bg-[#fafafa] p-3.5 text-[13.5px] leading-relaxed whitespace-pre-wrap">
                  {selected.prompt}
                </Card>
                <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-1.5">
                  Result
                </div>
                <Card className="gap-0 rounded-[10px] bg-[#fafafa] p-3.5 text-[13.5px] leading-relaxed whitespace-pre-wrap">
                  {selected.result}
                </Card>
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>
    </>
  );
}
