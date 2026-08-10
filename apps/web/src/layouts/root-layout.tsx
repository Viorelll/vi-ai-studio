import { Outlet } from "react-router-dom";
import { SiteHeader } from "@/components/site-header";
import { TooltipProvider } from "@/components/ui/tooltip";

export function RootLayout() {
  return (
    <TooltipProvider>
      <SiteHeader />
      <Outlet />
    </TooltipProvider>
  );
}
