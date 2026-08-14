## Batch 4 of 10 -- Database

Entities before endpoints, always -- batch 5 depends on this batch's IDs.

Produce, under `02-database/`:
- `00-conventions.md` (`DB-001`) -- naming, `IAuditable`/`ISoftDeletable` shape, UUID version, timestamp
  convention.
- `01-schema-map.md` (`DB-002`) -- one Postgres schema per bounded area (tenancy, identity, and one per
  selected functional area that owns data).
- `02-erd.md` (`DB-003`) -- entity relationship overview.
- `03-entities/*.md` -- **one file per aggregate group** (`DB-01x`), using the entity template, covering
  every noun named in the domain model (`PRD-005`) and every entity implied by the selected functional
  areas. Tenancy and identity entities always exist regardless of selection.
- `04-indexes-and-performance.md` (`DB-004`).
- `05-migrations.md` (`DB-005`).
- `06-seed-data.md` (`DB-006`).
- `07-soft-delete-and-retention.md` (`DB-019`) -- realize the interview's retention answers concretely.
- `08-row-level-security.md` (`DB-008`).

Every entity file's Acceptance criteria table MUST include a tenant-isolation row per the multi-tenancy
model (`ARCH-004`), unless the intake selected single-tenant.
