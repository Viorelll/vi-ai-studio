## Batch 5 of 10 -- Backend application and endpoints

Contracts derive from the entities (batch 4) and the permission matrix (`PRD-002`), never invented
fresh.

Only run this batch if `{{deployables}}` includes "HTTP API" (or an equivalent backend deployable).

Produce, under `03-apps/backend/`:
- `00-overview.md` (`BE-001`) through the per-concern specs implied by the selected functional areas
  and supporting infrastructure (authentication, authorization, tenant resolution, validation and
  errors, caching if Redis was selected, file storage if selected, notifications if selected,
  observability, health checks, rate limiting, testing, and an admin/impersonation spec only if
  "admin panel + impersonation" was selected).
- `endpoints/00-index.md` (`API-000`) -- a route table across every group below.
- `endpoints/*.md` -- one file per module named in the functional areas and product entities
  (auth, workspace/member, invitations if invites are relevant, and one group per top-level entity from
  `PRD-005`), using the endpoint template. Every collection endpoint states pagination style, sort
  allow-list and page-size cap (`{{consistency-rules}}`).

Selected functional areas for this product: {{functional_areas}}.
Do not write endpoint groups for functional areas that were not selected.
