import { Link } from "react-router-dom";
import { HomeIcon } from "lucide-react";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";

export interface BreadcrumbEntry {
  label: string;
  href?: string;
}

/**
 * Mirrors the design's breadcrumb row: a pill-style "Home" link,
 * then plain intermediate links, then the current page as bold static text.
 */
export function PageBreadcrumb({ items }: { items: BreadcrumbEntry[] }) {
  return (
    <Breadcrumb className="mb-4">
      <BreadcrumbList className="flex-nowrap">
        <BreadcrumbItem>
          <BreadcrumbLink
            render={<Link to="/" />}
            className="inline-flex items-center gap-1.5 rounded-full border bg-background px-3.5 py-1.5 font-semibold text-foreground no-underline hover:bg-muted hover:text-foreground"
          >
            <HomeIcon className="size-3.5" />
            Home
          </BreadcrumbLink>
        </BreadcrumbItem>
        {items.map((item, index) => {
          const isLast = index === items.length - 1;
          return (
            <span key={item.label} className="flex items-center gap-1.5">
              <BreadcrumbSeparator />
              <BreadcrumbItem>
                {isLast || !item.href ? (
                  <BreadcrumbPage className="font-bold">{item.label}</BreadcrumbPage>
                ) : (
                  <BreadcrumbLink render={<Link to={item.href} />} className="font-semibold">
                    {item.label}
                  </BreadcrumbLink>
                )}
              </BreadcrumbItem>
            </span>
          );
        })}
      </BreadcrumbList>
    </Breadcrumb>
  );
}
