import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { CheckIcon } from "lucide-react";
import { RecentGeneratedItem } from "@/components/recent-generated-item";
import { PageLoading } from "@/components/page-loading";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { useSpecifications } from "@/hooks/use-specifications";
import {
  useGeneratedProjects,
  useGeneratedProjectsWithVersions,
} from "@/hooks/use-generated";
import { formatDateTime } from "@/lib/format";
import { getStatusBadgeClassName, getStatusLabel } from "@/lib/status";
import type { SpecificationDetail, SpecificationSummary } from "@/lib/types";

export function LandingPage() {
  const specsQuery = useSpecifications();
  const generatedProjectsQuery = useGeneratedProjects();
  const specs = specsQuery.data ?? [];
  const generatedProjects = generatedProjectsQuery.data ?? [];
  const recentGenerated = useGeneratedProjectsWithVersions(
    generatedProjects.slice(0, 3),
  );

  if (specsQuery.isPending || generatedProjectsQuery.isPending) {
    return <PageLoading />;
  }

  const draftCount = specs.filter((s) => s.status === "draft").length;
  const buildingCount = specs.filter(
    (s) => s.latestGenerationStatus === "running",
  ).length;
  const readyCount = generatedProjects.filter(
    (s) => s.latestGenerationStatus === "ready",
  ).length;
  const failedCount = generatedProjects.filter(
    (s) => s.latestGenerationStatus === "failed",
  ).length;

  return (
    <main className="flex-1 flex justify-center px-7 pb-12 relative overflow-hidden">
      <div
        className="absolute top-0 left-0 right-0 h-[340px] pointer-events-none"
        style={{
          background:
            "linear-gradient(180deg, var(--background) 0%, var(--brand-tint) 70%, var(--brand-tint) 100%)",
        }}
      />
      <div className="w-full max-w-[1180px] relative z-10">
        <div className="flex flex-wrap items-end justify-between gap-10 pt-14 pb-10">
          <div className="max-w-[520px]">
            <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-3">
              AI Build Platform
            </div>
            <h1 className="text-[40px] font-extrabold leading-[1.05] tracking-tight mb-3">
              Vi - AI Studio
            </h1>
            <p className="text-[15px] text-muted-foreground leading-relaxed">
              Turn a specification into a running project, built by AI.
            </p>
          </div>

          <div className="flex gap-5 flex-wrap">
            <Card size="sm" className="px-5 py-4">
              <CardHeader className="p-0">
                <CardTitle className="flex items-center gap-1.5 text-[11px] font-bold text-muted-foreground uppercase tracking-wide">
                  <SpecIcon compact className="size-[13px]" />
                  Specifications
                </CardTitle>
              </CardHeader>
              <CardContent className="p-0 flex gap-5">
                <Stat label="Total" value={specs.length} />
                <Stat label="Draft" value={draftCount} />
                <Stat
                  label="Building"
                  value={buildingCount}
                  className="text-primary"
                />
              </CardContent>
            </Card>
            <Card
              size="sm"
              className="px-5 py-4 bg-[var(--brand-tint)] border-[var(--brand-tint-border)]"
            >
              <CardHeader className="p-0">
                <CardTitle className="flex items-center gap-1.5 text-[11px] font-bold text-[var(--brand)] uppercase tracking-wide">
                  <CheckIcon className="size-[13px]" />
                  Generated Projects
                </CardTitle>
              </CardHeader>
              <CardContent className="p-0 flex gap-5">
                <Stat label="Total" value={generatedProjects.length} />
                <Stat
                  label="Ready"
                  value={readyCount}
                  className="text-green-700 dark:text-green-500"
                />
                <Stat
                  label="Failed"
                  value={failedCount}
                  className="text-red-700 dark:text-red-500"
                />
              </CardContent>
            </Card>
          </div>
        </div>

        <div className="mx-1 mb-8 h-px bg-[linear-gradient(90deg,transparent,#d8e0da,transparent)]" />

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-5 mb-10">
          <NavCard
            to="/specifications"
            icon={<SpecIcon className="size-[18px]" />}
            title="Project Specifications"
            description="Browse existing specs, start a new one, and launch AI Build on any of them."
          />
          <NavCard
            to="/generated"
            icon={<CheckIcon className="size-[18px]" />}
            title="Generated Projects"
            description="See every project AI Build has finished and deployed."
            tinted
          />
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-6 pb-14">
          <RecentList
            title="Recently generated project specifications"
            items={specs.slice(0, 3)}
          />
          <RecentGeneratedList
            title="Recently generated projects"
            projects={recentGenerated.data}
          />
        </div>
      </div>
    </main>
  );
}

function Stat({
  label,
  value,
  className,
}: {
  label: string;
  value: number;
  className?: string;
}) {
  return (
    <div className="text-center min-w-[56px]">
      <div
        className={`text-[22px] font-extrabold tracking-tight ${className ?? ""}`}
      >
        {value}
      </div>
      <div className="text-[11.5px] text-muted-foreground mt-0.5">{label}</div>
    </div>
  );
}

function NavCard({
  to,
  icon,
  title,
  description,
  tinted,
}: {
  to: string;
  icon: ReactNode;
  title: string;
  description: string;
  tinted?: boolean;
}) {
  return (
    <Link to={to} className="group">
      <Card
        className={`h-full rounded-2xl p-7 gap-3.5 transition hover:shadow-md hover:ring-foreground/20 ${
          tinted
            ? "bg-[var(--brand-tint)] border-[var(--brand-tint-border)]"
            : ""
        }`}
      >
        <div className="flex items-center justify-center size-[38px] rounded-[9px] bg-muted text-[var(--brand)]">
          {icon}
        </div>
        <CardHeader className="p-0">
          <CardTitle className="text-[17px] font-bold">{title}</CardTitle>
        </CardHeader>
        <CardContent className="p-0 flex-1">
          <CardDescription className="text-[13px] leading-relaxed">
            {description}
          </CardDescription>
        </CardContent>
        <div className="text-[13px] font-semibold text-primary">
          View table →
        </div>
      </Card>
    </Link>
  );
}

function RecentGeneratedList({
  title,
  projects,
}: {
  title: string;
  projects: {
    spec: SpecificationSummary;
    generations: SpecificationDetail["generations"];
  }[];
}) {
  return (
    <div>
      <div className="text-[13px] font-semibold text-muted-foreground mb-3">
        {title}
      </div>
      <Card className="gap-0 p-0 py-0 overflow-hidden">
        {projects.length === 0 ? (
          <div className="px-4.5 py-4 text-sm text-muted-foreground">
            Nothing here yet.
          </div>
        ) : (
          projects.map(({ spec, generations }) => (
            <RecentGeneratedItem
              key={spec.id}
              spec={spec}
              generations={generations}
            />
          ))
        )}
      </Card>
    </div>
  );
}

function RecentList({
  title,
  items,
}: {
  title: string;
  items: SpecificationSummary[];
}) {
  return (
    <div>
      <div className="text-[13px] font-semibold text-muted-foreground mb-3">
        {title}
      </div>
      <Card className="gap-0 p-0 py-0 overflow-hidden">
        {items.length === 0 ? (
          <div className="px-4.5 py-4 text-sm text-muted-foreground">
            Nothing here yet.
          </div>
        ) : (
          items.map((item) => (
            <Link
              key={item.id}
              to={`/specifications/${item.id}`}
              className="flex h-12 items-center justify-between gap-3 border-b border-[#f0f0f1] px-4.5 last:border-b-0 hover:bg-muted/50"
            >
              <span className="min-w-0 flex-1 truncate text-[13px] font-semibold">
                {item.name}
              </span>
              <div className="flex shrink-0 items-center gap-3.5">
                <span className="text-xs text-muted-foreground whitespace-nowrap">
                  {formatDateTime(item.created)}
                </span>
                <Badge
                  variant="outline"
                  className={getStatusBadgeClassName(item.status)}
                >
                  {getStatusLabel(item.status)}
                </Badge>
              </div>
            </Link>
          ))
        )}
      </Card>
    </div>
  );
}

function SpecIcon({
  className,
  compact,
}: {
  className?: string;
  compact?: boolean;
}) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <rect x="3" y="4" width="18" height="16" rx="2" />
      <line x1="3" y1="10" x2="21" y2="10" />
      <line x1="8" y1="14" x2="14" y2="14" />
      {!compact && <line x1="8" y1="17" x2="11" y2="17" />}
    </svg>
  );
}
