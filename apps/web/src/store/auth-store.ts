import { create } from "zustand";
import { signInWithGoogle } from "@/api/auth";
import {
  AUTH_EXPIRED_EVENT,
  clearAuth,
  getAuth,
  setAuth,
  type AuthenticatedUser,
} from "@/api/auth-token";

interface AuthState {
  user: AuthenticatedUser | null;
  isAuthenticated: boolean;
  isSigningIn: boolean;
  error: string | null;
  signIn: (googleIdToken: string) => Promise<void>;
  signOut: () => void;
}

const initial = getAuth();

export const useAuthStore = create<AuthState>((set) => ({
  user: initial?.user ?? null,
  isAuthenticated: Boolean(initial),
  isSigningIn: false,
  error: null,
  signIn: async (googleIdToken) => {
    set({ isSigningIn: true, error: null });
    try {
      const result = await signInWithGoogle(googleIdToken);
      setAuth(result);
      set({ user: result.user, isAuthenticated: true, isSigningIn: false });
    } catch (error) {
      set({
        error:
          error instanceof Error ? error.message : "Google sign-in failed.",
        isSigningIn: false,
      });
    }
  },
  signOut: () => {
    clearAuth();
    set({ user: null, isAuthenticated: false, error: null });
  },
}));

if (typeof window !== "undefined") {
  window.addEventListener(AUTH_EXPIRED_EVENT, () => {
    useAuthStore.setState({
      user: null,
      isAuthenticated: false,
      isSigningIn: false,
      error: "Your session has expired. Sign in again.",
    });
  });
}
