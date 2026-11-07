# Hand photo finale implementation plan

**Goal:** Accept all photographic settings, lock the camera after answering the offered hand, add a subtle render-only approach, and enter the darkroom after durable final exposure.
**Spec:** User-approved design in this conversation (2026-11-07): any aperture/shutter combination; no photographic success gate; existing girl/player animation and gentle contact unchanged; finder/aperture/shutter only after response; commit before automatic transition.
**Architecture:** Keep story checkpoints and film persistence. Separate visual camera displacement from gameplay transforms and animation targets. Use the existing BrushSceneTransition and House darkroom entry route.
**Tech Stack:** Unity 2022.3, built-in renderer, existing C# partial controllers and Editor validation checks.

## Constraints and rulings
- Keep loading, finite film, pending-save protection, checkpoint order, and the meadow hand-response prerequisite. Remove quality/composition/focus/exposure gates, not story progression.
- Preserve source models, bones, girl presentation script, and the player's existing contact pose.
- Keep pause/focus suspension and retries. No scene exit on encoding or persistence failure.
- Camera advances 0.10 m with the existing reach progress; one 3 mm step with <=0.2 degree roll, then rests. Render-only displacement leaves bone/AI reference transforms unchanged.
- Dial navigation returns to finder; it cannot lower the camera or reset response progress. Resume saved joined checkpoints into the locked finder.
- Existing dirty workspace is shared; edit scoped files without resetting others. Tests use backup/restore of EditorPlaySession.
- Live-test ruling: the old return flag only opened a curtain and could load the default bedroom. Preserve that intent through asynchronous summer binding, then enter the authored darkroom view under transition cover (WBMain SharedHouse/SpringBedroom/DirectDarkroom).

## Tasks
- [x] Add failing isolated checks for arbitrary exposure acceptance, locked mode navigation, unchanged response timer and locked focus.
- [x] Remove photographic quality gates in Bridge/Assess; keep informational optical rendering.
- [x] Add response lock, render-only view, and shared progress/visibility support without changing poses.
- [x] Branch only after committed joined exposure into existing darkroom transition; hold contact during cover; retain retry on failure.
- [x] Run isolated camera/persistence checks, live story sequence at extreme settings, pause/resume and automatic darkroom entry. Capture actual rendered contact/transition and restore saves.
- [x] Review scoped diff, source hashes and Console; record evidence and limitations.

## Review focus
- Save failure and duplicate shutter must neither leave nor consume extra film.
- All mode-entry paths, including hotkeys, back, icon and loaded checkpoint, obey the same lock.
- Render transform restoration must survive repeated/manual Camera.Render and leave arm/girl transforms unchanged.
- Dial screens and transient input handoffs must not hide or reset the joined hand.
- Scene transition covers the unchanged contact before scene unload; old completed rolls still have a recoverable darkroom path.
