import { GoogleLogin } from "@react-oauth/google";
import { LoaderCircle, ShieldCheck, Sparkles } from "lucide-react";
import { useAuthStore } from "@/store/auth-store";

export function LoginPage() {
  const signIn = useAuthStore((state) => state.signIn);
  const isSigningIn = useAuthStore((state) => state.isSigningIn);
  const error = useAuthStore((state) => state.error);

  return (
    <main className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[#f5f7f5] px-6 py-12">
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_15%_20%,#dcebe3_0,transparent_34%),radial-gradient(circle_at_85%_80%,#e8e5dc_0,transparent_38%)]" />
      <div className="relative w-full max-w-[420px] rounded-[18px] border border-[#dfe5df] bg-white/95 p-8 shadow-[0_24px_70px_rgba(34,54,42,0.12)] sm:p-10">
        <div className="flex items-center gap-3">
          <span className="flex size-10 items-center justify-center rounded-[10px] bg-[var(--brand)] text-white">
            <Sparkles className="size-5" />
          </span>
          <div>
            <div className="text-[11px] font-semibold uppercase tracking-[0.16em] text-muted-foreground">
              Enterprise workspace
            </div>
            <h1 className="text-xl font-bold tracking-tight">Vi - AI Studio</h1>
          </div>
        </div>

        <div className="mt-12">
          <h2 className="text-[28px] font-extrabold leading-tight tracking-tight">
            Build the next version.
          </h2>
          <p className="mt-3 text-sm leading-relaxed text-muted-foreground">
            Sign in to turn product specifications into running projects with
            your team.
          </p>
        </div>

        <div className="mt-8">
          <div className="relative h-11 w-full overflow-hidden rounded-[9px]">
            <div className="pointer-events-none flex h-full items-center justify-center gap-2.5 rounded-[9px] border border-[#d9ded9] bg-white text-sm font-semibold text-foreground shadow-sm">
              <span className="text-lg font-extrabold leading-none text-[#4285f4]">
                G
              </span>
              Continue with Google
            </div>
            <div className="absolute inset-0 opacity-0">
              <GoogleLogin
                onSuccess={(credentialResponse) => {
                  if (credentialResponse.credential) {
                    void signIn(credentialResponse.credential);
                  }
                }}
                onError={() => {
                  useAuthStore.setState({ error: "Google sign-in failed." });
                }}
                size="large"
                width="420"
              />
            </div>
          </div>

          {isSigningIn && (
            <p className="mt-4 flex items-center justify-center gap-2 text-sm text-muted-foreground">
              <LoaderCircle className="size-4 animate-spin" />
              Signing in...
            </p>
          )}
          {error && (
            <p className="mt-4 text-center text-sm text-destructive">{error}</p>
          )}
        </div>

        <div className="mt-10 flex items-start gap-2 border-t border-[#edf0ed] pt-5 text-xs leading-relaxed text-muted-foreground">
          <ShieldCheck className="mt-0.5 size-4 shrink-0 text-[var(--brand)]" />
          <span>
            Your workspace access and role permissions are managed securely.
          </span>
        </div>
      </div>
    </main>
  );
}
