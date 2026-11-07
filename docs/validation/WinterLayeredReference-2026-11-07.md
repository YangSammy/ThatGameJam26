# Winter first-stage layered reference

Implemented the approved cream / pale-blue watercolor composition with six independently imported paintings in `Assets/Resources/WinterPaperArt/Layers`: Background, SnowPlatforms, RiverRocks, Foreground, Bridge and SwingFrame. The swing painting includes both ground supports. Ropes and the painted seat move separately; the bridge painting is revealed continuously from the chapter's growth state. Unacquired mechanisms use translucent painted hints.

`PaintingWorld3D.OutdoorLayers.cs` maps authoritative first-stage positions into the reference composition. Actor and swing share this mapping. Physics, save coordinates and the second-stage layout are not changed by this reconstruction. The character's sitting pose intentionally places its feet .18 units below the seat contact point.

## Verification

- Unity compilation console: no errors on the final check.
- `WinterPaperArtChecks.Run()`: PASS after correcting the test's standing-vs-sitting contact assumption. Eight isolated renders; six layer assets and transparent silhouettes; shared swing sitting contact; bridge growth binding; unchanged chapter checkpoints; 24 foot-aligned frames; both jump directions; stable riding and respawn poses; unacquired visibility; renderer restoration.
- Actual `Bedroom_Winter.unity` layout was opened additively and bound to a temporary presentation instance. Both stages rendered; actual route scale (1,1,1). Scene closed without saving.
- Inspected `actual-layout-0.png`, swing contact, half bridge, bridge contact, and undiscovered state. Fixed a shader mask bug that initially cropped all layers on the right: `_UseReveal` is now enabled only for the real bridge.
- Final read-only review: no outstanding blocking issue after the sitting-contact test correction.

Evidence: `output/QA/WinterPaperArt_20261107/`, including `checks.txt`, `01-outdoor.png` through `08-bridge-contact.png` (named states), and `actual-layout-0.png` / `actual-layout-1.png`.

This verifies Editor rendering and state bindings, not a fresh end-to-end live playthrough or standalone player build. The earlier broad narrative check had an unrelated ward retreat assertion conflicting with the existing D-only ward behavior; this change does not alter it. The composition follows the reference but is a layered repaint, not a pixel-identical cutout. The moving swing's angle follows the existing mechanism state.
