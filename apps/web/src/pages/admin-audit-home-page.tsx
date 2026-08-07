import { Link } from "react-router-dom";
import { AlignLeftIcon, Settings2Icon } from "lucide-react";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { Card } from "@/components/ui/card";

export function AdminAuditHomePage() {
  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[1000px]">
        <PageBreadcrumb items={[{ label: "Admin", href: "/admin" }, { label: "Audit" }]} />

        <div className="text-[15px] font-bold mb-0.5">Audit — AI model call logs</div>
        <p className="text-[13px] text-muted-foreground mb-6">
          Requests and token usage per specification and per generated build.
        </p>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <Link to="/admin/audit/specifications">
            <Card className="p-5.5 gap-3.5 h-full transition hover:shadow-md hover:ring-foreground/20">
              <div className="size-[34px] rounded-lg bg-muted flex items-center justify-center">
                <AlignLeftIcon className="size-4" />
              </div>
              <div className="text-[15px] font-bold">Project specifications</div>
              <p className="text-[12.5px] text-muted-foreground leading-relaxed">AI call logs across every specification.</p>
            </Card>
          </Link>
          <Link to="/admin/audit/generated">
            <Card className="p-5.5 gap-3.5 h-full transition hover:shadow-md hover:ring-foreground/20">
              <div className="size-[34px] rounded-lg bg-muted flex items-center justify-center">
                <Settings2Icon className="size-4" />
              </div>
              <div className="text-[15px] font-bold">Generated projects</div>
              <p className="text-[12.5px] text-muted-foreground leading-relaxed">AI call logs per generated build version.</p>
            </Card>
          </Link>
        </div>
      </div>
    </main>
  );
}
