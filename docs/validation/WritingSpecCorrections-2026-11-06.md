# Day 3 specification corrections — 2026-11-06

User's final direction takes precedence: retain orange outlines and rightward-only writing; remove the girl's missing-item pointing cue.

Changes:
- WalkSpeed 22 model units/sec. Recommended center -> pendant -> north flowers -> east letter -> southeast suitcase -> south entrance route takes 38.00s at 30/60 Hz, 37.97s at 120 Hz; about 22s of the 60s clock remains. This is automated uninterrupted input, not human usability timing.
- E tap buffers the nearby item for 0.22s. Releasing E does not immediately discard intent. Expiry, leaving range, successful pickup, input cancellation/retry discard it. Holding E remains supported. A new rightward ink movement must occur in the current simulation tick; the old 0.18s stale-ink allowance is removed.
- BouquetSatchelAnchor plus visible satchel holds the bouquet at the player's side independently of collecting luggage. Attachment follows turns.
- Girl pointing trigger and presentation removed. Legacy missingItemHintUsed checkpoint field retained but ignored on restore for compatibility.
- Orange TaskOutlineCue remains. Rightward-only stroke detection remains.
- Pendant already had pickup -> collar -> hidden animation. Close-up inspection found the previous destination partially buried in the torso; destination now stays in front of the chest before tucking inside. Collected ownership remains true when hidden.

Validation:
- Before fixes, tests failed on E buffering and route duration (5.00s).
- InputContract passed: buffered tap, expiry, stationary pen cannot pick up, held E with fresh ink, retired girl cue.
- Route checks passed at 30/60/120 Hz, all within 35-45 seconds.
- CarryPresentation passed before final visibility adjustment: pendant starts at dish, visible transfer, hidden but owned; flower parent/position follows satchel and turns.
- Narrative regression 12/12 and AutumnRestorationChecks model/presentation passed after logic changes and before the final pendant visibility-only adjustment.
- Final pendant visibility adjustment was compiled and rendered with the actual production world; close-ups: output/QA/MemoryBedroom/pendant-collar.png and pendant-tucked-bouquet.png.
- Editor temporarily entered Play Mode externally and tools timed out; after it returned to Edit Mode, final rerun passed: narrative 12/12, InputContract, all three route frame rates, strengthened CarryPresentation, restoration model and presentation. Console errors: 0. The external play session was not stopped by this task.
- git diff --check passed for changed runtime/check files. No Player build or manual two-handed usability session performed.
