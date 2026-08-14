## Batch 2 of 10 -- Product

Requirements get their IDs here; every later spec traces back to one of these files.

Produce, under `00-product/`:
- `00-vision-and-scope.md` (`PRD-001`) -- one paragraph vision, in scope / out of scope, from the
  domain interview's one-sentence description and journeys.
- `01-personas-and-roles.md` (`PRD-002`) -- one persona per actor named in the interview, plus a
  permission matrix: role x dangerous action, from the interview's actors-and-permissions answers.
  This matrix is the single source every later authorization statement must reference by permission
  key -- do not let any later batch invent a permission key not listed here.
- `02-functional-requirements.md` (`PRD-003`, one `FR-nnn` row per requirement) -- built from the
  selected functional areas plus the interview's journeys. Priority M/S/C.
- `03-non-functional-requirements.md` (`PRD-004`, one `NFR-nnn` row per requirement) -- numbers, not
  adjectives, taken directly from the interview's scale-and-budgets answers; adjust strictness to the
  selected rigour dial.
- `04-domain-model.md` (`PRD-005`) -- the core nouns and ownership relationships from the domain
  interview, as a conceptual model (no database types yet -- that is batch 4).
- `05-user-journeys.md` (`PRD-006`) -- the journeys from the interview, happy and unhappy path each.

Use the domain interview's vocabulary verbatim throughout, not generic SaaS language.
