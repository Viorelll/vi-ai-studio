import { useParams, useSearchParams } from "react-router-dom";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { BuildProgress } from "@/components/build-progress";
import { PageLoading } from "@/components/page-loading";
import { NotFoundView } from "@/components/not-found-view";
import { useSpecification } from "@/hooks/use-specifications";
import { ApiError } from "@/lib/api-client";

export function BuildPage() {
  const { specId } = useParams<{ specId: string }>();
  const [searchParams] = useSearchParams();
  const generationId = searchParams.get("generation");

  const specQuery = useSpecification(specId);

  if (specQuery.isPending) return <PageLoading />;
  if (specQuery.isError) {
    if (specQuery.error instanceof ApiError && specQuery.error.status === 404) {
      return <NotFoundView message="This specification doesn't exist." />;
    }
    throw specQuery.error;
  }
  if (!generationId) return <NotFoundView message="No build generation was specified." />;
  const spec = specQuery.data;

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[900px]">
        <PageBreadcrumb items={[{ label: "Project Specifications", href: "/specifications" }, { label: "AI Build" }]} />
        <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">AI Build</div>
        <h1 className="text-xl font-bold tracking-tight mt-0.5 mb-7">{spec.name}</h1>

        <BuildProgress generationId={generationId} />
      </div>
    </main>
  );
}
