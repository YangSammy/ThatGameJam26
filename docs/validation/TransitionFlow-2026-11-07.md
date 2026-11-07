# Transition flow — 2026-11-07

## Changes

- Follow-up: per user request, House to Park was restored to Brush. All single-scene destinations and standalone bedroom swaps now use Brush. The unused Noise resource remains available but is not selected by gameplay.
- Completed ending credits automatically request a saved return to the main menu. The final title/button page is removed. Failed save/navigation attempts retry every two seconds. The credits Canvas retains its white background below the transition overlay until unloading.
- Removed Park's manual pause state, legacy pause UI, Leave memory and Leave with saved progress APIs, and saved-only exit bypass. Esc still opens the shared pause menu, whose save/main-menu/quit controls remain.
- Completed rolls retain a dedicated Return to darkroom prompt. Back-click cannot dismiss it and strand the return action. Photography save failures retain shutter retry.
- Updated affected regression/character validation callers to use the shared pause menu; no scene/prefab authoring changes were needed.

## Verification

- Unity 2022.3.62f3 refreshed and compiled runtime and Editor scripts successfully. Final Console read had zero errors; four unused-field warnings remain in untouched GameScore and ParkPhotoPrototype.Controls fields.
- `python Tools/verify_brush_transitions.py`: 23 PASS assertions. Initial expanded resource check failed on missing Noise settings before implementation, then passed after adding the resource.
- `BrushTransitionChecks.AssetsAndRoutes()`: PASS for both settings resources, non-looping animations, and centralized scene navigation.
- `ParkExposureEncoderChecks.Run()` and `RunProductionIntegration()`: PASS for encoding/retry/cancellation, exactly-once commits, pending-write protection and scene-departure suppression, using isolated fixture storage.
- `ParkPhotoFlowChecks.PausedSave()`: PASS; completed encoding commits once during shared pause without advancing guided departure.
- Isolated preview-scene credits fixture: waits for scrolling, retains opaque background after completion, hides finished text, resets on restart.
- Isolated ending-host fixture: no return before credits finish; auto-return attempts without input after completion; failed saves retry after two seconds, not every frame. Intentionally unready host prevents disk writes and scene navigation.
- Isolated completed-roll input fixture: back action cannot dismiss completion prompt.
- Scoped `git diff --check`: PASS. Whole-Assets search found no remaining references to the removed manual-pause method/state or saved-only exit flag.
- Independent static code review found legacy validation reflection callers; these were migrated. Existing unrelated working changes were preserved against scoped pre-edit snapshots.

## Limits

No Play Mode traversal, full build, or visual acceptance run was performed. Noise/Brush appearance and the complete credits-to-menu scene traversal still need in-game acceptance. Existing production saves were not used by the checks. Scoped pre-edit copies are under `output/validation/transition-flow-20261107/before`.
