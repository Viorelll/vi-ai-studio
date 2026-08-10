import { Link } from "react-router-dom";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { NewProjectButton } from "@/components/new-project-button";
import { PageLoading } from "@/components/page-loading";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { Empty, EmptyDescription } from "@/components/ui/empty";
import { buttonVariants } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useSpecifications } from "@/hooks/use-specifications";
import { formatDate } from "@/lib/format";
import { getStatusBadgeClassName, getStatusLabel } from "@/lib/status";
import { cn } from "@/lib/utils";

export function SpecificationsListPage() {
  const specsQuery = useSpecifications();
  if (specsQuery.isPending) return <PageLoading />;
  const specs = specsQuery.data ?? [];

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[1220px]">
        <PageBreadcrumb items={[{ label: "Project Specifications" }]} />

        <div className="flex flex-wrap items-start justify-between gap-5 mb-7">
          <div>
            <h1 className="text-[22px] font-bold tracking-tight">
              Project Specifications
            </h1>
            <p className="text-sm text-muted-foreground mt-1">
              Author a spec, then let AI Build turn it into a running project.
            </p>
          </div>
          <NewProjectButton />
        </div>

        <Card className="rounded-[12px] bg-white p-0 overflow-hidden ring-1 ring-[#e4e4e7]">
          <Table className="table-fixed min-w-[980px]">
            <colgroup>
              <col style={{ width: "23.95%" }} />
              <col style={{ width: "10.18%" }} />
              <col style={{ width: "10.78%" }} />
              <col style={{ width: "9.58%" }} />
              <col style={{ width: "11.98%" }} />
              <col style={{ width: "19.16%" }} />
              <col style={{ width: "14.37%" }} />
            </colgroup>
            <TableHeader>
              <TableRow className="border-b border-[#e4e4e7] bg-[#fafafa] hover:bg-[#fafafa]">
                <TableHead className="h-auto px-5 py-3 text-[12px] font-semibold text-[#71717a]">
                  Project
                </TableHead>
                <TableHead className="h-auto px-5 py-3 text-[12px] font-semibold text-[#71717a]">
                  Status
                </TableHead>
                <TableHead className="h-auto px-5 py-3 text-[12px] font-semibold text-[#71717a]">
                  Owner
                </TableHead>
                <TableHead className="h-auto px-5 py-3 text-[12px] font-semibold text-[#71717a]">
                  Created
                </TableHead>
                <TableHead className="h-auto px-5 py-3 text-[12px] font-semibold text-[#71717a]">
                  Progress
                </TableHead>
                <TableHead className="h-auto px-5 py-3 text-[12px] font-semibold text-[#71717a]">
                  Stack
                </TableHead>
                <TableHead className="h-auto px-5 py-3 text-right text-[12px] font-semibold text-[#71717a]"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {specs.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} className="py-10">
                    <Empty className="min-h-0 border-0 p-0">
                      <EmptyDescription>
                        No specifications yet.
                      </EmptyDescription>
                    </Empty>
                  </TableCell>
                </TableRow>
              ) : (
                specs.map((spec) => (
                  <TableRow
                    key={spec.id}
                    className="border-b border-[#f0f0f1] hover:bg-[#fafafa]"
                  >
                    <TableCell className="min-w-0 px-5 py-4">
                      <Link
                        to={`/specifications/${spec.id}`}
                        className="block truncate text-[14px] font-semibold hover:underline"
                      >
                        {spec.name}
                      </Link>
                      <div className="mt-0.5 truncate text-[12px] text-zinc-400">
                        {spec.summary}
                      </div>
                    </TableCell>
                    <TableCell className="px-5 py-4">
                      <Badge
                        variant="outline"
                        className={getStatusBadgeClassName(spec.status)}
                      >
                        {getStatusLabel(spec.status)}
                      </Badge>
                    </TableCell>
                    <TableCell className="px-5 py-4 text-[13px] text-[#3f3f46]">
                      {spec.owner}
                    </TableCell>
                    <TableCell className="whitespace-nowrap px-5 py-4 text-[13px] text-[#71717a]">
                      {formatDate(spec.created)}
                    </TableCell>
                    <TableCell className="px-5 py-4">
                      <div className="flex items-center gap-2">
                        <Progress
                          value={spec.progress}
                          className={cn(
                            "h-1.5 w-[60px] shrink-0",
                            spec.status === "failed"
                              ? "[&_[data-slot=progress-indicator]]:bg-red-600"
                              : spec.status === "ready"
                                ? "[&_[data-slot=progress-indicator]]:bg-green-600"
                                : "[&_[data-slot=progress-indicator]]:bg-zinc-900",
                          )}
                        />
                        <span className="whitespace-nowrap text-[12px] text-[#71717a]">
                          {spec.progress}%
                        </span>
                      </div>
                    </TableCell>
                    <TableCell className="min-w-0 px-5 py-4">
                      <div className="flex min-w-0 flex-nowrap items-center gap-1.5 overflow-hidden">
                        {(() => {
                          const stack = [
                            spec.stack.backend,
                            spec.stack.ui,
                            spec.stack.database,
                            spec.stack.infra,
                          ];
                          const [first, ...rest] = stack;
                          return (
                            <>
                              <Badge
                                variant="secondary"
                                className="max-w-[110px] truncate rounded-md border border-[#e4e4e7] bg-[#f4f4f5] px-2 py-0.5 text-[11px] font-normal text-[#3f3f46]"
                              >
                                {first}
                              </Badge>
                              {rest.length > 0 && (
                                <Badge
                                  variant="outline"
                                  className="rounded-md border-[#e4e4e7] bg-white px-2 py-0.5 text-[11px] font-normal text-zinc-400"
                                >
                                  +{rest.length}
                                </Badge>
                              )}
                            </>
                          );
                        })()}
                      </div>
                    </TableCell>
                    <TableCell className="px-5 py-4 text-right">
                      {spec.status === "draft" ? (
                        <Link
                          to={`/specifications/${spec.id}/launch`}
                          className={cn(
                            buttonVariants({ variant: "outline", size: "sm" }),
                            "h-7 rounded-md px-2.5 text-[12px] font-semibold",
                          )}
                        >
                          Start Studio
                        </Link>
                      ) : (
                        <span
                          className={cn(
                            buttonVariants({ variant: "outline", size: "sm" }),
                            "opacity-45 cursor-not-allowed pointer-events-none",
                          )}
                        >
                          Start Studio
                        </span>
                      )}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </Card>
      </div>
    </main>
  );
}
