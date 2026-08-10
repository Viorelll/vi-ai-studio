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
      <BreadcrumbList className="flex-wrap">
        <BreadcrumbItem>
          <BreadcrumbLink
            render={<Link to="/" />}
            className="inline-flex items-center gap-1.5 rounded-full border border-[#e4e4e7] bg-white py-[7px] pr-3.5 pl-2.5 text-[13px] font-semibold text-[#3f3f46] no-underline hover:bg-[#f4f4f5] hover:text-[#3f3f46]"
          >
            <HomeIcon className="size-3.5" />
            Home
          </BreadcrumbLink>
        </BreadcrumbItem>
        {items.map((item, index) => {
          const isLast = index === items.length - 1;
          return (
            <span
              key={item.label}
              className="flex min-w-0 items-center gap-1.5"
            >
              <BreadcrumbSeparator className="text-[13px] text-[#d4d4d8]">
                ›
              </BreadcrumbSeparator>
              <BreadcrumbItem>
                {isLast || !item.href ? (
                  <BreadcrumbPage className="max-w-[min(60vw,520px)] truncate text-[13px] font-bold">
                    {item.label}
                  </BreadcrumbPage>
                ) : (
                  <BreadcrumbLink
                    render={<Link to={item.href} />}
                    className="max-w-[min(60vw,520px)] truncate text-[13px] font-semibold"
                  >
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
