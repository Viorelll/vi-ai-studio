import { Link } from "react-router-dom";
import { StatusBadge } from "@/components/status-badge";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { NewProjectButton } from "@/components/new-project-button";
import { PageLoading } from "@/components/page-loading";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { buttonVariants } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useSpecifications } from "@/hooks/use-specifications";
import { cn } from "@/lib/utils";

export function SpecificationsListPage() {
  const specsQuery = useSpecifications();
  if (specsQuery.isPending) return <PageLoading />;
  const specs = specsQuery.data ?? [];

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[1220px]">
        <PageBreadcrumb items={[{ label: "Project Specifications" }]} />

        <div className="flex items-start justify-between mb-7">
          <div>
            <h1 className="text-[22px] font-bold tracking-tight">Project Specifications</h1>
            <p className="text-sm text-muted-foreground mt-1">
              Author a spec, then let AI Build turn it into a running project.
            </p>
          </div>
          <NewProjectButton />
        </div>

        <Card className="p-0 overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Project</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Owner</TableHead>
                <TableHead>Created</TableHead>
                <TableHead>Progress</TableHead>
                <TableHead>Stack</TableHead>
                <TableHead className="text-right"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {specs.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-muted-foreground py-10">
                    No specifications yet.
                  </TableCell>
                </TableRow>
              ) : (
                specs.map((spec) => (
                  <TableRow key={spec.id}>
                    <TableCell>
                      <Link to={`/specifications/${spec.id}`} className="font-semibold hover:underline">
                        {spec.name}
                      </Link>
                      <div className="text-xs text-muted-foreground">{spec.summary}</div>
                    </TableCell>
                    <TableCell>
                      <StatusBadge status={spec.status} />
                    </TableCell>
                    <TableCell className="text-sm">{spec.owner}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {new Date(spec.created).toLocaleDateString("en-US")}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <Progress
                          value={spec.progress}
                          className={cn(
                            "w-16 h-1.5",
                            spec.status === "failed"
                              ? "[&_[data-slot=progress-indicator]]:bg-red-600"
                              : spec.status === "ready"
                                ? "[&_[data-slot=progress-indicator]]:bg-green-600"
                                : "[&_[data-slot=progress-indicator]]:bg-zinc-900"
                          )}
                        />
                        <span className="text-xs text-muted-foreground">{spec.progress}%</span>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1.5 flex-wrap items-center">
                        {(() => {
                          const stack = [spec.stack.backend, spec.stack.ui, spec.stack.database, spec.stack.infra];
                          const [first, ...rest] = stack;
                          return (
                            <>
                              <Badge variant="secondary" className="font-normal">
                                {first}
                              </Badge>
                              {rest.length > 0 && (
                                <Badge variant="outline" className="font-normal text-muted-foreground">
                                  +{rest.length}
                                </Badge>
                              )}
                            </>
                          );
                        })()}
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      {spec.status === "draft" ? (
                        <Link
                          to={`/specifications/${spec.id}/launch`}
                          className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
                        >
                          Start Studio
                        </Link>
                      ) : (
                        <span
                          className={cn(buttonVariants({ variant: "outline", size: "sm" }), "opacity-45 cursor-not-allowed pointer-events-none")}
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
