# Handoff: Vi - AI Studio → Next.js + shadcn/ui + Tailwind

## Overview
Vi - AI Studio is a SaaS tool for authoring detailed software specifications through a 15-phase wizard, generating code from those specs via AI ("AI Build"), and browsing/administering the results (generated project versions, AI model configuration, audit logs).

## About the design files
The bundled `Vi AI Studio (reference).dc.html` (+ `support.js`) is a **design reference prototype**, built in an internal HTML templating format — not production code. Do not copy its markup or its runtime (`support.js`) into the app. The task is to **recreate this UI in Next.js (App Router) using shadcn/ui components and Tailwind CSS**, following the target stack's conventions (Server/Client Components, `cn()` utility, shadcn primitives, TanStack Query or route handlers for data, etc).

Data in the prototype is all dummy/in-memory (defined in a `<script>` block near the bottom of the file, e.g. `initialSpecs()`, `TEAM_COLORS`, `TASK_DEFS`). Treat it as sample data shape, not a data layer to port.

## Fidelity
**High-fidelity.** Colors, type sizes/weights, spacing, radii, and copy below are taken directly from the prototype's inline styles — recreate them pixel-accurately with Tailwind utilities (arbitrary values where the exact px value isn't on the default scale, e.g. `text-[13px]`, `rounded-[14px]`).

## Design tokens

**Font:** Inter (400/500/600/700/800), loaded from Google Fonts. Monospace accents use `'SF Mono', ui-monospace, monospace` (version tags, token counts, code-like values) — map to Tailwind's `font-mono`.

**Base palette (neutrals, Tailwind zinc/gray scale):**
- Background: `#fafafa` (app canvas), `#ffffff` (cards, header)
- Borders: `#e4e4e7`, hairlines `#f0f0f1` / `#f5f5f6`
- Text: primary `#09090b`/`#18181b`, secondary `#3f3f46`, tertiary `#71717a`, faint `#a1a1aa`
- Hover surface: `#f4f4f5`

**Status pill colors:**
- Draft: bg `#f4f4f5` / text `#52525b` / border `#e4e4e7`
- Building: bg `#eff6ff` / text `#1d4ed8` / border `#bfdbfe`
- Ready: bg `#f0fdf4` / text `#15803d` / border `#bbf7d0`
- Failed: bg `#fef2f2` / text `#b91c1c` / border `#fecaca`

**Team accent colors** (global switcher, 5 swatches — drives buttons, progress bars, icons, hero gradient tint, links app-wide):
| Key | Color | Tint bg | Tint border |
|---|---|---|---|
| Slate | `#18181b` | `#f4f4f5` | `#e4e4e7` |
| Indigo | `#4f46e5` | `#eef0fd` | `#d7dafa` |
| Emerald | `#059669` | `#eefbf4` | `#cdeedd` |
| Amber | `#b45309` | `#fdf3e7` | `#f2ddb8` |
| Rose | `#e11d48` | `#fdedf0` | `#f8ccd5` |

Implement as a single `accent` value (React context or Zustand store, persisted client-side) that resolves to these token sets; apply via Tailwind arbitrary-value classes or CSS variables set on `<html>`/`<body>` (e.g. `--accent`, `--accent-tint`, `--accent-tint-border`) so shadcn's `Button`/`Badge`/`Progress` variants can reference them.

**Radii:** small controls 6–8px, cards 10–14px, chips/pills/avatars `9999px` (full).
**Shadows:** cards use a subtle hover-only shadow `0 4px 16px rgba(0,0,0,0.06)` with border-color darkening to `#a1a1aa` — no resting shadow.
**Type scale:** 40px/800 (hero title) · 22px/700 (page titles) · 17–20px/700 (section/card titles) · 14–15px/600 · 13–13.5px/500–600 (body/labels) · 11–12.5px/500–700 (meta, badges, table headers), letter-spacing -0.02 to -0.03em on large headings.

## Suggested shadcn/ui component mapping
- Top nav bar → custom header (shadcn doesn't ship one), `Avatar` for the "JD" initials
- Team color swatches → custom small button row (no direct shadcn equivalent)
- Stat cards, spec/project cards → `Card`
- Status chips → `Badge` (custom color variants per status above)
- Tables (Specifications, Audit logs, AI configs) → shadcn `Table`
- Progress bars (spec progress, wizard progress, build progress) → `Progress`
- Breadcrumbs → shadcn `Breadcrumb`
- Expandable version history / audit rows → `Collapsible` or `Accordion`
- Log detail popup → `Dialog`
- Add-configuration inline form → plain `Card` + shadcn `Input`/`Select`/`Label`, or `Sheet` if you prefer a slide-over
- Wizard phase list (left rail) + step checklist + keyword chips → custom layout; chips as toggleable `Badge`/`Button` (`variant="outline"` ↔ selected `variant="default"`)
- Markdown preview panel → `<pre>`/`ScrollArea` with monospace text
- Service status pills (API/AI Generator/Storage) → `Badge` with colored dot
- File tree (56-file spec tree, generated project tree) → custom recursive list component, folder/file `lucide-react` icons (`Folder`, `File`)
- Build log console → dark `Card` (`bg-zinc-950`) with `ScrollArea`, colored log lines
- "Download .zip" — reproduce with a real zip library (e.g. `jszip` client-side, or a server route) using the prototype's dummy file list as content

## Screens / views
1. **Landing (Home)** — landscape hero, split stat cards (Specifications: Total/Draft/Building; Generated Projects: Total/Ready/Failed), two nav cards (Project Specifications / Generated Projects), two recent-activity lists (recent specs, recent generated projects with expandable version chips).
2. **Project Specifications (dashboard)** — breadcrumb, "+ New Project" button, table: Project/Status/Owner/Created/Progress/Stack chips/Action (Start Studio or Stop Build).
3. **Generated Projects** — breadcrumb, list of expandable project cards; each expands into a vertical timeline of versions (version tag, Latest badge, model used as note, status, date, build duration, stack chips) + "Open specification" link.
4. **Version files** — breadcrumb, project name + version + model, Download .zip + View specification buttons, generated file tree (folders/files, indentation).
5. **Spec detail** — breadcrumb, name + status badge, summary, 4 metadata tiles (Owner/Created/Progress/Audience), Description, Requirements & features, Tech stack chips, 56-file specification folder tree (`/specification`, `/business`, `/requirements`, `/personas`, `/features`, `/user-stories`, `/use-cases`, `/domain`, `/api`, `/database`, `/security`, `/architecture`, `/frontend`, `/testing`, `/devops`, `/tasks`) + Download .zip, primary action button (Start Studio / Start AI Build) at bottom-right.
6. **Start AI Build (Launch)** — back link, spec name, card: model `<select>`, stack chips, full-width "Generate" button.
7. **AI Build (progress)** — breadcrumb, vertical step tracker (spinning icon on active step) + build progress bar + dark console log panel; "Done — back to dashboard" button on completion.
8. **AI Specification Studio (wizard)** — breadcrumb, top progress bar across all 15 phases, two-column layout: left rail = project-basics form (name, summary, stack selects — most options disabled as "(coming soon)" except .NET/Next.js/PostgreSQL/Docker/Tailwind) + scrollable 15-phase list with per-phase completion counts; right pane = active phase: phase number/title/output description, step-item checklist toggle, keyword chips (toggleable), live `.md` preview `<pre>` block with "Generate" button, Back/Next footer. Technical Design phase additionally shows a static layered architecture diagram (Frontend → API → Application → Domain → Infrastructure → PostgreSQL/Redis/Blob storage).
9. **Admin Dashboard home** — breadcrumb, 3 service-status pills (colored dot + name), 3 nav cards (AI model configuration, Task Routing implied within it, Audit).
10. **Admin → AI model configuration** — "+ Add configuration" reveals inline form (Label/Provider/Model name/Base URL/API key); table of configs with masked API key + Reveal toggle, Edit/Delete row actions; below it, Task Routing grid (Code/Image/Sound/Transcribe cards, each with a model `<select>`).
11. **Admin → Audit home** — breadcrumb, 2 cards: Project Specs / Generated Projects.
12. **Admin → Audit list** (specs mode: flat table of Specification/Log entries/Total requests/Total tokens; generated mode: expandable project rows → nested version table with Version/Generated/Log entries/Requests/Tokens).
13. **Admin → Audit detail** — spec/version name, summary, 3 metric tiles (Total requests/Tokens in/Tokens out), log table (Time/Model/Task/Requests/Tokens in-out) — row click opens a **Dialog** with full prompt + result text.

## Interactions & behavior
- Breadcrumbs (`Home › Section › Detail`) replace all back buttons except in the wizard/launch pages, which use a plain "← Back"/✕ close.
- Team color swatch click sets the global accent instantly across hero gradient, buttons, progress bars, icons, links, active badges.
- All list/table rows are clickable (`cursor:pointer`, subtle `#fafafa` hover background); cards get a hover border-darken + soft shadow.
- Expand/collapse rows (Generated Projects, recent-generated list, Audit generated list) use a chevron glyph.
- Wizard: each phase item toggle is a step-by-step checklist (one item active at a time), keyword chips are multi-select toggles, "Generate" produces the markdown preview text, Next/Back move between phases; progress bar reflects overall phase completion.
- AI Build page: step tracker cycles through stages with a spinning icon on the active step (`@keyframes vispin` full rotation) and a `@keyframes vipulse` opacity pulse elsewhere; progress bar animates width via `transition: width 0.3–0.4s ease`; console auto-scrolls as log lines append.
- Admin AI config: "Reveal" toggles a masked API key; Edit prefills the inline form; Delete removes the row (confirm dialog recommended in production even though the prototype doesn't have one).
- Log detail dialog closes on backdrop click or × button.

## State management
Key state to model in the real app:
- Current route/view (map 1:1 to Next.js routes, e.g. `/`, `/specifications`, `/specifications/[id]`, `/generated`, `/generated/[id]/versions/[version]`, `/studio/[specId]`, `/build/[specId]`, `/admin`, `/admin/ai-config`, `/admin/audit`, `/admin/audit/[mode]/[id]`)
- Global `accentColor` (team color key) — persist in localStorage or a cookie, apply via CSS vars/context
- Specs collection (id, name, summary, status, owner, created, progress, stack, description, features, audience, generations[])
- Generated project versions per spec (version, date, duration, model, status, stack)
- Wizard state per spec: 15 phases → each with checklist items, selected keyword chips, generated markdown
- Admin: AI model configs list (label, provider, model, baseUrl, apiKey — mask by default), task→model routing map (Code/Image/Sound/Transcribe), audit log entries (time, model, task, requests, tokensIn, tokensOut, prompt, result)
- UI-only state: expanded/collapsed rows, open dialog id, add-config form visibility/edit-mode

## Assets
No image assets — all icons are inline stroke-style SVGs (24×24 viewBox, `stroke-width:2`, `round` caps/joins) equivalent to `lucide-react` icons (folder, file, checkmark, sliders/settings, chevron). Use `lucide-react` (shadcn's default icon set) 1:1 in place of the inline SVGs.

## Files
- `Vi AI Studio (reference).dc.html` + `support.js` — the full design reference (all 13 screens, dummy data, styles) to read while rebuilding. Open in a browser to interact with it directly.
