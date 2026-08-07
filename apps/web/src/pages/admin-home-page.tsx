import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { Settings2Icon, CircleCheckBigIcon } from "lucide-react";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { PageLoading } from "@/components/page-loading";
import { Card } from "@/components/ui/card";
import { useServices } from "@/hooks/use-admin";

export function AdminHomePage() {
  const servicesQuery = useServices();
  if (servicesQuery.isPending) return <PageLoading />;
  const services = servicesQuery.data ?? [];

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[1000px]">
        <PageBreadcrumb items={[{ label: "Admin" }]} />

        <div className="mb-6">
          <h1 className="text-[22px] font-bold tracking-tight">Admin Dashboard</h1>
          <p className="text-sm text-muted-foreground mt-1">Configure AI providers and route each task to a model.</p>
        </div>

        <div className="flex gap-2.5 flex-wrap mb-6">
          {services.map((service) => (
            <div
              key={service.name}
              className="flex items-center gap-2 rounded-full px-3.5 py-2 text-[13px] font-semibold text-white"
              style={{ backgroundColor: service.online ? "#16a34a" : "#dc2626" }}
            >
              <span className="size-[7px] rounded-full bg-white" />
              {service.name}
            </div>
          ))}
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <AdminNavCard
            to="/admin/ai-config"
            icon={<Settings2Icon className="size-4" />}
            title="AI model configuration"
            description="Providers, models, API keys, and task routing."
          />
          <AdminNavCard
            to="/admin/audit"
            icon={<CircleCheckBigIcon className="size-4" />}
            title="Audit"
            description="AI call logs per specification: requests and token usage."
          />
        </div>
      </div>
    </main>
  );
}

function AdminNavCard({
  to,
  icon,
  title,
  description,
}: {
  to: string;
  icon: ReactNode;
  title: string;
  description: string;
}) {
  return (
    <Link to={to}>
      <Card className="p-5.5 gap-3.5 h-full transition hover:shadow-md hover:ring-foreground/20">
        <div className="size-[34px] rounded-lg bg-muted flex items-center justify-center">{icon}</div>
        <div className="text-[15px] font-bold">{title}</div>
        <p className="text-[12.5px] text-muted-foreground leading-relaxed">{description}</p>
      </Card>
    </Link>
  );
}
