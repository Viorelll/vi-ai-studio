import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { GeneratedProjectCard } from "@/components/generated-project-card";
import { PageLoading } from "@/components/page-loading";
import { useGeneratedProjects, useGeneratedProjectsWithVersions } from "@/hooks/use-generated";

export function GeneratedProjectsPage() {
  const generatedProjectsQuery = useGeneratedProjects();
  const projectsWithVersions = useGeneratedProjectsWithVersions(generatedProjectsQuery.data ?? []);

  if (generatedProjectsQuery.isPending || projectsWithVersions.isPending) return <PageLoading />;
  const projects = projectsWithVersions.data;

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[1220px]">
        <PageBreadcrumb items={[{ label: "Generated Projects" }]} />

        <div className="mb-7">
          <h1 className="text-[22px] font-bold tracking-tight">Generated Projects</h1>
          <p className="text-sm text-muted-foreground mt-1">Projects AI Build has finished and deployed.</p>
        </div>

        <div className="flex flex-col gap-3.5">
          {projects.length === 0 ? (
            <div className="text-center text-muted-foreground py-16 border rounded-xl bg-card">
              No generated projects yet -- start a build from a specification.
            </div>
          ) : (
            projects.map(({ spec, generations }) => <GeneratedProjectCard key={spec.id} spec={spec} generations={generations} />)
          )}
        </div>
      </div>
    </main>
  );
}
