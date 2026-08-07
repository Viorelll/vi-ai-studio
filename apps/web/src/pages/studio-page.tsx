import { useParams } from "react-router-dom";
import { StudioWizard } from "@/components/studio-wizard";
import { PageLoading } from "@/components/page-loading";
import { NotFoundView } from "@/components/not-found-view";
import { useSpecification } from "@/hooks/use-specifications";
import { ApiError } from "@/lib/api-client";

export function StudioPage() {
  const { specId } = useParams<{ specId: string }>();
  const specQuery = useSpecification(specId);

  if (specQuery.isPending) return <PageLoading />;
  if (specQuery.isError) {
    if (specQuery.error instanceof ApiError && specQuery.error.status === 404) {
      return <NotFoundView message="This specification doesn't exist." />;
    }
    throw specQuery.error;
  }

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <StudioWizard spec={specQuery.data} />
    </main>
  );
}
