import { Link, useParams } from "react-router-dom";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { AuditGeneratedList } from "@/components/audit-generated-list";
import { PageLoading } from "@/components/page-loading";
import { NotFoundView } from "@/components/not-found-view";
import { Card } from "@/components/ui/card";
import { Empty, EmptyDescription } from "@/components/ui/empty";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useSpecifications } from "@/hooks/use-specifications";
import { useGeneratedProjects } from "@/hooks/use-generated";
import { useAuditRollups } from "@/hooks/use-audit";
import type { AiCallLogRollup, SpecificationSummary } from "@/lib/types";

export function AdminAuditListPage() {
  const { mode } = useParams<{ mode: string }>();
  const isValidMode = mode === "specifications" || mode === "generated";

  const rollupsQuery = useAuditRollups();
  const specsQuery = useSpecifications();
  const generatedQuery = useGeneratedProjects();

  if (!isValidMode) return <NotFoundView />;
  if (
    rollupsQuery.isPending ||
    specsQuery.isPending ||
    generatedQuery.isPending
  )
    return <PageLoading />;

  const rollups = rollupsQuery.data ?? [];
  const title =
    mode === "specifications" ? "Project specifications" : "Generated projects";

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[1000px]">
        <PageBreadcrumb
          items={[
            { label: "Admin", href: "/admin" },
            { label: "Audit", href: "/admin/audit" },
            { label: title },
          ]}
        />

        <div className="text-[15px] font-bold mb-3.5">{title}</div>

        {mode === "specifications" ? (
          <SpecsAuditTable specs={specsQuery.data ?? []} rollups={rollups} />
        ) : (
          <AuditGeneratedList
            specs={generatedQuery.data ?? []}
            rollups={rollups}
          />
        )}
      </div>
    </main>
  );
}

function SpecsAuditTable({
  specs,
  rollups,
}: {
  specs: SpecificationSummary[];
  rollups: AiCallLogRollup[];
}) {
  const rollupById = new Map(rollups.map((r) => [r.specificationId, r]));

  return (
    <Card className="p-0 overflow-hidden">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Specification</TableHead>
            <TableHead>Log entries</TableHead>
            <TableHead>Total requests</TableHead>
            <TableHead>Total tokens</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {specs.length === 0 ? (
            <TableRow>
              <TableCell colSpan={4} className="py-10">
                <Empty className="min-h-0 border-0 p-0">
                  <EmptyDescription>No specifications yet.</EmptyDescription>
                </Empty>
              </TableCell>
            </TableRow>
          ) : (
            specs.map((spec) => {
              const rollup = rollupById.get(spec.id);
              return (
                <TableRow key={spec.id}>
                  <TableCell>
                    <Link
                      to={`/admin/audit/specifications/${spec.id}`}
                      className="font-semibold hover:underline"
                    >
                      {spec.name}
                    </Link>
                    <div className="text-xs text-muted-foreground">
                      {spec.summary}
                    </div>
                  </TableCell>
                  <TableCell className="text-sm">
                    {rollup?.logCount ?? 0}
                  </TableCell>
                  <TableCell className="text-sm">
                    {rollup?.totalRequests ?? 0}
                  </TableCell>
                  <TableCell className="text-sm font-mono">
                    {(rollup?.totalTokens ?? 0).toLocaleString("en-US")}
                  </TableCell>
                </TableRow>
              );
            })
          )}
        </TableBody>
      </Table>
    </Card>
  );
}
