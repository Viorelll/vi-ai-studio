import { Link } from "react-router-dom";
import { LogOut } from "lucide-react";
import { buttonVariants } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { AccentSwitcher } from "@/components/accent-switcher";
import { cn } from "@/lib/utils";
import { useAuthStore } from "@/store/auth-store";

export function SiteHeader() {
  const user = useAuthStore((state) => state.user);
  const signOut = useAuthStore((state) => state.signOut);
  const isAdmin = user?.roles.includes("Admin") ?? false;
  const displayName = user?.fullName || user?.email || "Account";

  return (
    <header className="h-14 flex-none flex items-center justify-between px-7 border-b bg-background">
      <Link to="/" className="flex items-center gap-2.5">
        <span className="h-7 w-7 rounded-[7px] bg-[var(--brand)] flex items-center justify-center">
          <svg
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            className="text-white"
          >
            <path
              d="M4 5l8 14 8-14"
              stroke="currentColor"
              strokeWidth="2.2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
            <circle cx="19" cy="6.2" r="2.1" fill="currentColor" />
          </svg>
        </span>
        <span className="text-sm font-semibold tracking-tight">
          Vi - AI Studio
        </span>
        <Badge
          variant="outline"
          className="ml-1 h-auto rounded-full border-[#e4e4e7] px-2 py-0.5 text-[11px] font-normal leading-[1.2] text-[#71717a]"
        >
          Enterprise
        </Badge>
      </Link>
      <div className="flex items-center gap-3.5 text-sm">
        {/*
          Base UI's Button enforces button semantics and its docs explicitly
          say links shouldn't be rendered through Button's `render` prop --
          style the <Link> directly with buttonVariants instead.
        */}
        {isAdmin && (
          <Link
            to="/admin"
            className={cn(
              buttonVariants({ variant: "outline" }),
              "h-[31px] rounded-[7px] px-3.5 text-[13px] font-semibold text-[#3f3f46]",
            )}
          >
            Admin dashboard
          </Link>
        )}
        <AccentSwitcher />
        <span className="max-w-[180px] truncate text-[13px] text-[#52525b]">
          {displayName}
        </span>
        <Avatar size="sm" className="bg-[#e4e4e7] text-[#3f3f46]">
          <AvatarImage src={user?.avatarUrl ?? undefined} alt="" />
          <AvatarFallback className="bg-[#e4e4e7] text-xs font-semibold text-[#3f3f46]">
            {getInitials(displayName)}
          </AvatarFallback>
        </Avatar>
        <button
          type="button"
          onClick={signOut}
          aria-label="Sign out"
          title="Sign out"
          className="inline-flex size-8 items-center justify-center rounded-lg text-[#71717a] transition hover:bg-muted hover:text-foreground"
        >
          <LogOut className="size-4" />
        </button>
      </div>
    </header>
  );
}

function getInitials(displayName: string) {
  const initials = displayName
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toUpperCase();
  return initials || "U";
}
