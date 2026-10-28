# v3 darkroom — manual preparation and liquid changes

Approved direction: continue v3 by priority, preserve scene assets and anchors, implementation only without runtime validation. Reference: complete v3 design, page 7. This increment closes skipped rewind and skipped liquid removal before further darkroom expansion.

## Changes

1. New v3 rolls entering the darkroom start exposed, not already in the dark bag. Hold the camera rewind control and scroll; after rewinding, open the back and drag the cartridge to the bag.
2. Hold the bag and scroll to load the reel. Click the raised tank lid to commit sealing before chemistry is allowed. No exposed negative is rendered outside the bag.
3. Pour the correct bottle onto the tank, agitate and wait for its existing timer. After each of developer, stop and fixer, drag the tank over the existing tap/sink and release to drain. Wrong drops return it without clearing the liquid. The next bottle and rinse are blocked while spent liquid remains.
4. Rewind amount, open back, sealed tank and pending drainage join the existing atomic roll checkpoint. Cancelling a drag restores the prop, does not drain, and requires a fresh press after input handoff.

## Compatibility

The darkroom subrecord uses workflowVersion 1. Existing darkroom records retain workflow 0 and their previous steps; developed rolls remain viewable. Only v3 rolls with no darkroom record start the new preparation flow. Frame files and park layout are unchanged. The existing procedural processing props are reused; the lid is attached to the tank so it follows tank manipulation.

## Next priority

Water wash still uses the prior timer and opens the prior gallery. Drying, contact-print exposure and paper processing are NOT implemented in this increment. Next implement these physical steps and delay final chapter completion until the paper process is committed. Afterwards continue v3 autumn writing, winter mechanics, and ending.

## Validation status

Not compiled, played, built or tested, per user request. Pending checks include lid hit/drop behavior, tank drain cancellation, all save/resume boundaries, and old-roll compatibility. Durations remain the existing whitebox values.
