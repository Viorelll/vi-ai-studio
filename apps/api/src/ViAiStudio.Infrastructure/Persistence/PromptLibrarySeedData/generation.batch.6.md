## Batch 6 of 10 -- Frontend application and screens

Screens consume the endpoint contracts from batch 5 -- do not invent a screen for an endpoint that
does not exist, and do not invent an endpoint here.

Only run this batch if the selected frontend is not "no UI (API only)".

Produce, under `03-apps/frontend/`:
- `00-overview.md` (`FE-001`) through the per-concern specs: design tokens, Tailwind/theming (if
  Tailwind-based), component library, layouts and navigation, API client and state, forms and
  validation, i18n (if multi-language was selected), accessibility, error/empty/loading states,
  testing.
- `screens/00-index.md` (`UI-000`) -- a route table across every screen group below.
- `screens/*.md` -- one file per screen group (auth, onboarding/dashboard, one per top-level product
  entity from `PRD-005`, settings/admin), using the screen template. Every screen states its data
  dependencies by endpoint ID from batch 5, its loading/empty/error/forbidden states, and its
  keyboard-accessibility behaviour per the selected frontend requirements.

Selected frontend requirements: {{frontend_requirements}}.
