import { Link } from "react-router-dom";
import { buttonVariants } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { AccentSwitcher } from "@/components/accent-switcher";
import { cn } from "@/lib/utils";

export function SiteHeader() {
  return (
    <header className="h-14 flex-none flex items-center justify-between px-7 border-b bg-background">
      <Link to="/" className="flex items-center gap-2.5">
        <span className="h-7 w-7 rounded-[7px] bg-[var(--brand)] flex items-center justify-center">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" className="text-white">
            <path d="M4 5l8 14 8-14" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" />
            <circle cx="19" cy="6.2" r="2.1" fill="currentColor" />
          </svg>
        </span>
        <span className="text-sm font-semibold tracking-tight">Vi - AI Studio</span>
        <Badge variant="outline" className="ml-1 font-normal text-muted-foreground">
          Enterprise
        </Badge>
      </Link>
      <div className="flex items-center gap-3.5 text-sm">
        {/*
          Base UI's Button enforces button semantics and its docs explicitly
          say links shouldn't be rendered through Button's `render` prop --
          style the <Link> directly with buttonVariants instead.
        */}
        <Link to="/admin" className={cn(buttonVariants({ variant: "outline" }))}>
          Admin dashboard
        </Link>
        <AccentSwitcher />
        <span className="text-[13px] text-muted-foreground">John Doe</span>
        <span className="size-7 rounded-full bg-muted flex items-center justify-center text-xs font-semibold text-foreground/80">
          JD
        </span>
      </div>
    </header>
  );
}
