# Player 3C and tape interaction implementation plan

**Goal:** Stabilize exploration and input ownership, then polish targeted spindle operation, existing break discovery and tape replacement.
**Architecture:** Extend RoomWorld movement and existing WBMain/TapeSceneController gates. Preserve the authored scenes, radio deck, SafeInspection viewer and TapeChapter rules. Model position remains authoritative; presentation interpolates toward it.
**Spec:** Approved conversation design: relaxed walking (initial 1.8 m/s), modest acceleration with quick stop, collision/occlusion checks, exact exit camera return, no input leaking across modes. E inspection remains non-destructive; spindle selected by clicked surface, not mouse button. Discover existing damage and replace tape; no repair tools or cutting/gluing. Preserve known damage and scanned progress during room re-entry. No Computer Use.

- [x] Capture failing movement/collision/input regression and baseline Console.
- [x] Add configurable walking/sensitivity, deterministic movement boundary with short collision substeps and live blocker bounds; add entry occlusion and release gates.
- [x] Verify 3C and return poses before tape changes.
- [x] Add failing spindle-input regression. Use left-held actual spindle ID for scroll; block transport and unrelated holds. Keep legacy generated-world controls isolated.
- [x] Smooth authored cassette docking/return and spindle position; add existing break discovery cue/hint and repeatable safe swap. Keep source model intact through inspection and interruption.
- [x] Add spindle wheel support within inspection (display only), preserving chord ownership and zoom priority. Move destructive level restart out of the R shortcut into explicit help confirmation.
- [x] Update relevant tests and docs; run HouseScene/TapeScene, review, and restore HouseScene edit mode.

Validation: Unity MCP Play Mode handlers, compile/Console, authored camera captures. No native mouse claims or standalone build claims without evidence. Backups: QA/Backups/before-player-tape-polish. No project-wide refactor, save-format change or scene rebuild. Story/audio content remains placeholder.

Completed: movement/input 14, return poses 60, legacy tape polish 18, legacy recording 42 and close view 20 checks passed before routing migration. New user direction then superseded the legacy tape presentation: the new desk radio is now the sole player-facing device. See 2026-10-27-radio-unification.md and QA/radio-unification-validation.txt for current acceptance results. Legacy implementation and backups are retained; no build or native input validation is claimed.

