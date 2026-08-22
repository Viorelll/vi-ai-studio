import { Routes, Route } from "react-router-dom";
import { RootLayout } from "@/layouts/root-layout";
import { LandingPage } from "@/pages/landing-page";
import { SpecificationsListPage } from "@/pages/specifications-list-page";
import { SpecificationDetailPage } from "@/pages/specification-detail-page";
import { LaunchPage } from "@/pages/launch-page";
import { GeneratedProjectsPage } from "@/pages/generated-projects-page";
import { VersionFilesPage } from "@/pages/version-files-page";
import { StudioPage } from "@/pages/studio-page";
import { BuildPage } from "@/pages/build-page";
import { AdminHomePage } from "@/pages/admin-home-page";
import { AdminAiConfigPage } from "@/pages/admin-ai-config-page";
import { AdminAuditHomePage } from "@/pages/admin-audit-home-page";
import { AdminAuditListPage } from "@/pages/admin-audit-list-page";
import { AdminAuditDetailPage } from "@/pages/admin-audit-detail-page";
import { AdminAuditLogDetailPage } from "@/pages/admin-audit-log-detail-page";
import { NotFoundView } from "@/components/not-found-view";
import { LoginPage } from "@/pages/login-page";
import { useAuthStore } from "@/store/auth-store";

export function App() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  if (!isAuthenticated) {
    return <LoginPage />;
  }

  return (
    <Routes>
      <Route element={<RootLayout />}>
        <Route index element={<LandingPage />} />
        <Route path="specifications" element={<SpecificationsListPage />} />
        <Route
          path="specifications/:id"
          element={<SpecificationDetailPage />}
        />
        <Route path="specifications/:id/launch" element={<LaunchPage />} />
        <Route path="generated" element={<GeneratedProjectsPage />} />
        <Route
          path="generated/:id/versions/:version"
          element={<VersionFilesPage />}
        />
        <Route path="studio/:specId" element={<StudioPage />} />
        <Route path="build/:specId" element={<BuildPage />} />
        <Route path="admin" element={<AdminHomePage />} />
        <Route path="admin/ai-config" element={<AdminAiConfigPage />} />
        <Route path="admin/audit" element={<AdminAuditHomePage />} />
        <Route path="admin/audit/:mode" element={<AdminAuditListPage />} />
        <Route
          path="admin/audit/:mode/:id"
          element={<AdminAuditDetailPage />}
        />
        <Route
          path="admin/audit/:mode/:id/logs/:logId"
          element={<AdminAuditLogDetailPage />}
        />
        <Route path="*" element={<NotFoundView />} />
      </Route>
    </Routes>
  );
}
