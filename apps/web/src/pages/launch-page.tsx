import { Link, useParams } from "react-router-dom";
import { LaunchForm } from "@/components/launch-form";
import { PageLoading } from "@/components/page-loading";
import { NotFoundView } from "@/components/not-found-view";
import { useSpecification } from "@/hooks/use-specifications";
import { useAiConfigs, useTaskRouting } from "@/hooks/use-admin";
import { ApiError } from "@/lib/api-client";

export function LaunchPage() {
  const { id } = useParams<{ id: string }>();
  const specQuery = useSpecification(id);
  const configsQuery = useAiConfigs();
  const routingQuery = useTaskRouting();

  if (specQuery.isPending || configsQuery.isPending || routingQuery.isPending) return <PageLoading />;
  if (specQuery.isError) {
    if (specQuery.error instanceof ApiError && specQuery.error.status === 404) {
      return <NotFoundView message="This specification doesn't exist." />;
    }
    throw specQuery.error;
  }
  const spec = specQuery.data;
  const configs = configsQuery.data ?? [];
  const routing = routingQuery.data ?? [];
  const codeGenRouting = routing.find((r) => r.task === "codeGeneration");

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[560px]">
        <Link to={`/specifications/${spec.id}`} className="text-[13px] text-muted-foreground hover:text-foreground mb-4 inline-block">
          ← Back
        </Link>
        <h1 className="text-[22px] font-bold tracking-tight mb-1">Start AI Build</h1>
        <p className="text-sm text-muted-foreground mb-7">{spec.name}</p>

        <LaunchForm
          specId={spec.id}
          stack={spec.stack}
          configs={configs}
          defaultConfigId={codeGenRouting?.aiModelConfigId ?? null}
        />
      </div>
    </main>
  );
}
