import { QueryClient } from "@tanstack/react-query";
import { ApiError } from "@/lib/api-client";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Don't retry 4xx responses (e.g. 404 on a missing spec/generation) --
      // those are queries -- retrying just delays the NotFound view.
      retry: (failureCount, error) => {
        if (error instanceof ApiError && error.status >= 400 && error.status < 500) return false;
        return failureCount < 2;
      },
    },
  },
});
