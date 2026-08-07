namespace ViAiStudio.Domain.Catalog;

/// <summary>
/// The fixed, ordered set of phases every specification walks through in
/// AI Specification Studio. This is the server-side source of truth for the
/// wizard: the web client renders it, <see cref="Entities.SpecificationPhase"/>
/// rows reference it by index, and AI Build stage-generation prompts are
/// built from it.
/// </summary>
public static class SpecificationPhaseCatalog
{
    public static IReadOnlyList<SpecificationPhaseDefinition> Phases { get; } =
    [
        new(0, "Business Design", ["Product Vision", "Business Model", "Stakeholders", "Business Processes"],
            "Vision document, Business Model Canvas, stakeholder matrix, BPMN diagrams"),
        new(1, "Requirements", ["Functional Requirements", "Non-functional Requirements"],
            "Requirements Specification, NFR document"),
        new(2, "Product Design", ["User Roles", "User Stories", "Use Cases", "Acceptance Criteria"],
            "User stories, use cases & acceptance criteria"),
        new(3, "Domain Design", ["Entities", "Relationships"],
            "Domain Model, ER Diagram"),
        new(4, "Feature Design", ["Authentication", "Catalog", "Cart", "Orders", "Payments", "Notifications", "Reporting", "Administration"],
            "Module breakdown: features → screens → API → database"),
        new(5, "UX Design", ["User Journey", "Wireframes"],
            "UX specification"),
        new(6, "UI Design", ["Navigation", "Buttons", "Forms", "Tables", "Dialogs", "Responsive behavior", "Dark mode", "Loading states", "Empty states", "Errors"],
            "Complete UI in Figma"),
        new(7, "Technical Design", ["Frontend (Next.js)", "API (.NET)", "Application Layer", "Domain Layer", "Infrastructure", "PostgreSQL", "Redis", "Storage"],
            "Architecture diagrams"),
        new(8, "Database Design", ["Tables", "Relationships", "Indexes", "Constraints", "Migrations"],
            "ERD, SQL scripts"),
        new(9, "API Design", ["POST /auth/login", "GET /products", "GET /products/{id}", "POST /orders", "GET /orders", "POST /payments"],
            "OpenAPI Specification"),
        new(10, "Security Design", ["Authentication", "Authorization", "Permissions", "Encryption", "Audit logs", "Rate limiting", "OWASP", "Secrets"],
            "Security specification"),
        new(11, "Infrastructure", ["Cloud", "Docker", "Kubernetes", "CI/CD", "Monitoring", "Logging", "Backup", "Disaster Recovery"],
            "Infrastructure diagrams"),
        new(12, "Development", ["Frontend", "Backend", "Database", "Infrastructure"],
            "Working implementation"),
        new(13, "Quality Assurance", ["Unit tests", "Integration tests", "E2E tests", "Performance tests", "Security tests", "Accessibility tests"],
            "Test coverage across all layers"),
        new(14, "Deployment", ["Production", "Monitoring", "Logging", "Metrics", "Alerts", "Health Checks"],
            "Production deployment with monitoring & alerting"),
    ];

    public static SpecificationPhaseDefinition ByIndex(int index) =>
        index >= 0 && index < Phases.Count
            ? Phases[index]
            : throw new ArgumentOutOfRangeException(nameof(index), index, $"No phase at index {index}; valid range is 0-{Phases.Count - 1}.");
}
