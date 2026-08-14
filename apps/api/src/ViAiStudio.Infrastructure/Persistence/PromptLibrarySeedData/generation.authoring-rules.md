# Spec authoring rules

These specs are executed by a code-generating agent. Prose that reads well to a human but leaves a
choice open will produce a coin flip in the generated code. Write to remove choices.

## 1. Every spec file has YAML front-matter

```yaml
---
id: BE-004                      # unique, see the ID scheme
title: Authentication
component: apps/backend         # folder path, or "meta"
status: draft                   # draft | ready | implemented | superseded
version: 1.0
depends_on: [ARCH-005, DB-011]  # spec IDs that must be read first
provides: [auth.login, auth.refresh]   # capability keys other specs can reference
generates:                      # file globs this spec is responsible for
  - src/Api/Modules/Auth/**
  - tests/Api.IntegrationTests/Auth/**
---
```

`generates` matters: it makes ownership of a file unambiguous. Two specs must never claim the same
path.

## 2. Normative language

- **MUST / MUST NOT** -- non-negotiable. A violation fails review.
- **SHOULD** -- do it unless the spec gives a documented reason not to.
- **MAY** -- genuinely optional; the agent picks and records the pick.

Never use "consider", "ideally", "try to", "it would be nice". They generate nothing.

## 3. Every spec ends with acceptance criteria

```markdown
## Acceptance criteria

| # | Given | When | Then |
|---|---|---|---|
| AC-1 | a locked account | POST /auth/login with correct password | 423 with `error.code = auth.account_locked` |
```

Each row MUST be implementable as one automated test. "Works correctly" is not a row.

## 4. Contracts are shown, not described

Request/response bodies, table columns, message envelopes and component props are given as fenced
code blocks with concrete types. Never "returns the user object" -- show the object.

## 5. Failure paths are first-class

For each endpoint, job and consumer, list what happens on: invalid input, missing permission, not
found, concurrent modification, dependency unavailable, duplicate delivery, cancellation.

A spec with only the happy path is `status: draft` by definition.

## 6. No orphan requirements

Every functional requirement in `00-product/02-functional-requirements.md` MUST be referenced by at
least one implementing spec.

## 7. Size limit

A spec file over ~400 lines SHOULD be split. Agents work better with narrow, complete documents than
with one large one.

## 8. No code, except contracts

Specs contain schemas, signatures, DTOs, SQL DDL fragments and configuration keys. They do not
contain implementations. If you find yourself writing a method body, you are writing code in the
wrong place.
