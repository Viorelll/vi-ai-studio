## Template: a user-interface screen (or screen group)

```markdown
# Screen: `<Name>`

**Route.** `/<path>` * **Layout.** `AppShell` * **Guard.** authenticated + permission `<key>`
**Breakpoints.** behaviour at `sm` / `md` / `lg` described below.

## Job of this screen

One sentence, from the user's side of the screen.

## Data dependencies

| Data | Source | Query key | Stale time |
|---|---|---|---|
| <list> | `GET /api/v1/<module>` (`<API-ID>`) | `['<module>', tenantId, filters]` | 30 s |

## Layout

```
+ AppShell -------------------------------------+
| PageHeader: title + primary action            |
| FilterBar: search, status select, sort        |
| DataTable / CardGrid (md+ table, sm cards)     |
| Pagination: cursor "Load more"                 |
+-------------------------------------------------+
```

## States

| State | Trigger | Presentation |
|---|---|---|
| Loading (first) | no cached data | skeleton rows, count = last known page size or 5 |
| Loading (refetch) | background revalidate | keep data, show subtle top progress bar |
| Empty (no data) | 0 items, no filters | illustration + one-sentence explanation + primary CTA |
| Empty (filtered) | 0 items, filters set | "No results for these filters" + Clear filters |
| Error | request failed | inline `ErrorState` with retry; never a blank page |
| Forbidden | 403 | `NotAuthorized` panel, no navigation dead end |

## Interactions

| Trigger | Behaviour | Optimistic? |
|---|---|---|
| Click primary action | open create dialog | -- |
| Submit dialog | POST, invalidate the list query, toast confirmation | no |

## Copy

| Element | String key | English |
|---|---|---|
| Page title | `<module>.title` | <Title> |
| Primary action | `<module>.create` | New <item> |

## Accessibility

- Focus moves to the dialog title on open and returns to the trigger on close.
- Table has a caption; sortable headers expose `aria-sort`.
- All interactive elements reachable and operable by keyboard alone.

## Acceptance criteria

| # | Given | When | Then |
|---|---|---|---|
| AC-1 | user without the create permission | screen loads | primary action is not rendered (not merely disabled) |
```
