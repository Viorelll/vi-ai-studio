## Template: a group of HTTP endpoints

```markdown
# `<Module>` endpoints

Base route: `/api/v1/<module>` * All routes inherit the module's shared conventions.

## Summary

| Method | Route | Permission | Idempotent | Rate limit policy |
|---|---|---|---|---|
| GET | `/api/v1/<module>` | `<module>.read` | yes | `authenticated-read` |

---

## `POST /api/v1/<module>`

**Purpose.** One sentence.

**Auth.** Bearer token. Permission `<module>.create`. Tenant from token claim `tenant_id`.

**Request**

```jsonc
{
  "name": "string, required, 1..200, trimmed",
  "description": "string|null, max 4000"
}
```

**Validation**

| Field | Rule | Error code |
|---|---|---|
| `name` | required, 1-200 chars, unique per tenant (case-insensitive) | `validation.required`, `<module>.name_taken` |

**Response `201 Created`**

```jsonc
{ "id": "uuid", "name": "string", "createdAtUtc": "2026-01-01T00:00:00Z" }
```

`Location: /api/v1/<module>/{id}`

**Errors**

| Status | `error.code` | When |
|---|---|---|
| 400 | `validation.failed` | Any validation rule fails; `errors[]` carries field paths |
| 403 | `authz.forbidden` | Caller lacks `<module>.create` |
| 409 | `<module>.name_taken` | Unique constraint violated |

**Side effects**

- Writes audit entry `<module>.created`
- Publishes outbox event `<Module>CreatedIntegrationEvent` v1 (only if an integration-events
  architecture exists for this product)

**Acceptance criteria**

| # | Given | When | Then |
|---|---|---|---|
| AC-1 | authenticated user with permission | valid body | 201, row exists, one audit entry, all in one transaction |
| AC-2 | same name already used in the tenant | valid body | 409 `<module>.name_taken`, no rows written |
| AC-3 | name used in a *different* tenant | valid body | 201 -- uniqueness is per tenant |
```
