import { Link } from "react-router-dom";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export function NotFoundView({ message = "We couldn't find what you were looking for." }: { message?: string }) {
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-3 py-24 text-center">
      <div className="text-lg font-bold">Not found</div>
      <p className="text-sm text-muted-foreground max-w-sm">{message}</p>
      <Link to="/" className={cn(buttonVariants({ variant: "outline" }), "mt-2")}>
        Back to home
      </Link>
    </div>
  );
}
