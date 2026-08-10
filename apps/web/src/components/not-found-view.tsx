import { Link } from "react-router-dom";
import { buttonVariants } from "@/components/ui/button";
import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyTitle,
} from "@/components/ui/empty";
import { cn } from "@/lib/utils";

export function NotFoundView({
  message = "We couldn't find what you were looking for.",
}: {
  message?: string;
}) {
  return (
    <Empty className="flex-1 border-0 py-24">
      <EmptyHeader>
        <EmptyTitle className="text-lg font-bold">Not found</EmptyTitle>
        <EmptyDescription className="max-w-sm">{message}</EmptyDescription>
      </EmptyHeader>
      <EmptyContent>
        <Link
          to="/"
          className={cn(buttonVariants({ variant: "outline" }), "mt-2")}
        >
          Back to home
        </Link>
      </EmptyContent>
    </Empty>
  );
}
