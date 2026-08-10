import { apiClient } from "@/lib/api-client";
import type { StoredAuth } from "@/api/auth-token";

export function signInWithGoogle(idToken: string) {
  return apiClient.post<StoredAuth>("/api/auth/google", { idToken });
}
