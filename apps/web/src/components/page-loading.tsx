import { Spinner } from "@/components/ui/spinner";

export function PageLoading() {
  return (
    <div className="flex flex-1 items-center justify-center gap-2 py-24 text-sm text-muted-foreground">
      <Spinner />
      Loading…
    </div>
  );
}
