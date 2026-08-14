## Batch 10 of 10 -- Delivery

The final content batch. `manifest.md` is regenerated automatically after this batch from every
document's front-matter -- do not write it yourself.

Produce, under `06-delivery/`:
- `00-build-order.md` (`DEL-001`) -- the order a coding agent should implement this specification set
  in, phased by dependency (product -> architecture -> database -> backend -> frontend -> remaining
  deployables -> infrastructure -> quality), scaled to the selected team size: {{team}}.
- `01-claude-code-workflow.md` (`DEL-002`) -- how to work one spec at a time with a coding agent against
  this tree: read the manifest, load one spec plus its `depends_on` closure, implement, verify against
  its acceptance criteria, move on.

Selected specification scope: {{spec_scope}}. If the scope is "platform template only", keep this
batch minimal (build order and workflow only, no product-specific prompts). For "full product" or
narrower scopes, the build order should name the concrete modules this specification set actually
contains.
