import { PageBreadcrumb } from "@/components/page-breadcrumb";
import { AiConfigManager } from "@/components/ai-config-manager";

export function AdminAiConfigPage() {
  return (
    <main className="flex-1 flex justify-center px-7 py-10">
      <div className="w-full max-w-[1000px]">
        <PageBreadcrumb items={[{ label: "Admin", href: "/admin" }, { label: "AI model configuration" }]} />
        <AiConfigManager />
      </div>
    </main>
  );
}
