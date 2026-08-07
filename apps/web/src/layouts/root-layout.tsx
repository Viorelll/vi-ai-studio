import { Outlet } from "react-router-dom";
import { SiteHeader } from "@/components/site-header";

export function RootLayout() {
  return (
    <>
      <SiteHeader />
      <Outlet />
    </>
  );
}
