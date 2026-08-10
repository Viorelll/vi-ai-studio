import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import {
  CircleCheckBigIcon,
  ExternalLinkIcon,
  Settings2Icon,
} from "lucide-react";
import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { Card } from "@/components/ui/card";
import { useServiceStatuses } from "@/hooks/use-admin";

const SERVICES = [
  { name: "API", endpoint: "http://localhost:5081" },
  { name: "AI Generator", endpoint: "http://localhost:8100" },
  {
    name: "Storage (MinIO)",
    endpoint: "http://localhost:9001",
    mode: "no-cors",
  },
] as const;

export function AdminHomePage() {
  const serviceStatuses = useServiceStatuses(SERVICES);

  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[1000px]">
        <PageBreadcrumb items={[{ label: "Admin" }]} />

        <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
          <div>
            <h1 className="text-[22px] font-bold tracking-tight">
              Admin Dashboard
            </h1>
            <p className="text-sm text-muted-foreground mt-1">
              Configure AI providers and route each task to a model.
            </p>
          </div>
        </div>

        <div className="flex gap-2.5 flex-wrap mb-6">
          {SERVICES.map((service) => (
            <a
              key={service.name}
              href={service.endpoint}
              target="_blank"
              rel="noreferrer"
              title={`Open ${service.name} endpoints`}
              className="flex items-center gap-2 rounded-full px-3.5 py-2 text-[13px] font-semibold text-white transition-opacity hover:opacity-85 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              style={{
                backgroundColor:
                  serviceStatuses[service.name] === undefined
                    ? "#64748b"
                    : serviceStatuses[service.name]
                      ? "#16a34a"
                      : "#dc2626",
              }}
            >
              <span className="size-[7px] rounded-full bg-white" />
              {service.name}
              <ExternalLinkIcon className="size-3.5" />
            </a>
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
        <div className="size-[34px] rounded-lg bg-muted flex items-center justify-center">
          {icon}
        </div>
        <div className="text-[15px] font-bold">{title}</div>
        <p className="text-[12.5px] text-muted-foreground leading-relaxed">
          {description}
        </p>
      </Card>
    </Link>
  );
}
