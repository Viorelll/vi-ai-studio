## Output shape

Include a folder only if the intake sheet calls for it -- do not generate a scheduler folder for a
product with no scheduled work, and do not generate a payments spec that was not selected.

```
spec/
  README.md, CHANGELOG.md, glossary.md, manifest.md
  _meta/              authoring rules, ID scheme, traceability matrix, templates
  00-product/         vision, personas + permission matrix, functional requirements,
                       non-functional requirements with numeric targets, domain model, journeys
  01-architecture/     system context, solution structure, coding conventions, tenancy, security,
                       error model, integration events, configuration, observability, adr/
  02-database/         conventions, schema map, ERD, 03-entities/ (one file per aggregate group),
                       indexes, migrations, seed data, soft delete + retention, row-level security
  03-apps/<name>/       one folder per deployable from the intake sheet, each with an overview,
                       per-concern specs, and endpoints/ or screens/ or jobs as appropriate
  04-infrastructure/   environments, compose, images, proxy + TLS + secrets, observability stack,
                       CI/CD, backup + DR, local developer setup
  05-quality/          testing strategy, performance budgets, security checklist, definition of done
  06-delivery/         build order, agent workflow
```
