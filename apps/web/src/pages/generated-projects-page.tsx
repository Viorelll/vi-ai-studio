import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { GeneratedProjectCard } from "@/components/generated-project-card";
import { PageLoading } from "@/components/page-loading";
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyTitle,
} from "@/components/ui/empty";
import {
  useGeneratedProjects,
  useGeneratedProjectsWithVersions,
} from "@/hooks/use-generated";

export function GeneratedProjectsPage() {
  const generatedProjectsQuery = useGeneratedProjects();
  const projectsWithVersions = useGeneratedProjectsWithVersions(
    generatedProjectsQuery.data ?? [],
  );

  if (generatedProjectsQuery.isPending || projectsWithVersions.isPending)
    return <PageLoading />;
  const projects = projectsWithVersions.data;

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[1220px]">
        <PageBreadcrumb items={[{ label: "Generated Projects" }]} />

        <div className="mb-7">
          <h1 className="text-[22px] font-bold tracking-tight">
            Generated Projects
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Projects AI Build has finished and deployed.
          </p>
        </div>

        <div className="flex flex-col gap-3.5">
          {projects.length === 0 ? (
            <Empty className="rounded-[12px] border-[#e4e4e7] bg-white py-16">
              <EmptyHeader>
                <EmptyTitle>No generated projects yet</EmptyTitle>
                <EmptyDescription>
                  Start a build from a specification to see it here.
                </EmptyDescription>
              </EmptyHeader>
            </Empty>
          ) : (
            projects.map(({ spec, generations }) => (
              <GeneratedProjectCard
                key={spec.id}
                spec={spec}
                generations={generations}
              />
            ))
          )}
        </div>
      </div>
    </main>
  );
}
