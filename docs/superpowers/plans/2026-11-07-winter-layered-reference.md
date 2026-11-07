# First winter stage: layered reference reconstruction

User explicitly approved replacing the block-based result with independently painted background, snow platforms, river/rocks, foreground, bridge and animated swing; retain two supports.

- Generate true-alpha layers against the supplied composition; retain each source in Resources/WinterPaperArt/Layers.
- Use full-canvas alignment and a monotonic presentation coordinate map for the first stage so the bridge/platform proportions follow the reference rather than the old block layout. Physics and saves remain in their original coordinates. Keep second stage unchanged.
- Keep static swing frame separate from moving seat/ropes. Bind those to the current swing state; bridge reveal gets a continuous texture mask.
- Validate transparent assets, mapped contacts, state invariance, partial reveal, hidden/discovered states, actual scene anchors and rendered compositions. Record limitations honestly.

Completed: all layers imported, mechanism bindings integrated, eight isolated render checks passed, actual scene binding renders inspected, final read-only review completed. See `docs/validation/WinterLayeredReference-2026-11-07.md` for evidence and limits.
