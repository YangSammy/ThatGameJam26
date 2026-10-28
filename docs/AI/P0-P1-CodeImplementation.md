# P0 / P1 code-only increment — 2026-10-28

Scope: existing code only. No scene, prefab, material, layout or anchor edits. Preserve the parallel first-person-arm work. No Unity execution, Play tests or build performed in this increment; source inspection only. This is not full P0/P1 acceptance.

## Implemented

- Editor skip mode uses `five-day-development-v1.json` and permits independent checkpoints/completion without filling prerequisite completion flags. Normal chapter order stays sequential. Editor persistence with reset disabled now uses `EditorPersistentSession`; reset-enabled sessions still use `EditorPlaySession`. Standalone saves retain the original location. Existing old files are preserved, not migrated or deleted. Choose the skip mode before Play; switching profiles requires restarting Play.
- `WBMain.ChapterEntryBlockReason` and its Editor context menu report initialization, save, order, interaction and ray-hit blockers. They do not bypass corrupt saves or physical occlusion.
- Recording initialization explicitly requests unloaded audio data; clicking retries unavailable initialization and reports the reason. This addresses an initialization failure path, not a confirmed diagnosis of the reported HouseScene click failure.
- Three sequential tape commissions replace one-shot chapter completion. `HouseRadioDeck.commissionClips` accepts music one, music two and birthday audio. Until assigned, existing cassette clips remain temporary fallback sources. The current 14-second synthetic-noise commission builder is still a prototype and is not final narrative audio authoring.
- Each commission has its own PCM file, source hash and A/B cursors. Restored transports remain stopped. Source mismatch blocks loading without overwriting the recording. Old version-1 PCM is readable; new writes use version 2. The first commission retains the previous filename. Target preparation remains reusable between commissions. Passing the third commission completes the chapter.
- New/manual darkroom progress uses workflow 2. After negative rinsing: click tank to begin drying, hold the existing print surface to expose, drag bottles 1/2/3 to that surface in order for paper development/stop/fix, click tap to wash, click paper to dry. Only the last saved stage marks the roll Developed and the chapter complete. Early release/cancel during exposure returns to ContactReady; processing stages resume their saved remaining time. These are temporary bindings to existing props, not final trays or an exposure frame.
- Previously developed rolls remain viewable. Unfinished workflow-1 tank progress upgrades to workflow 2; legacy workflow-0 darkroom records retain old behavior. No earlier negatives are recaptured or overwritten.
- Photography route diagnostics check required task points, walking order and authored meadow contact reach. No anchors are moved. Current focus/aperture/shutter settings are checkpointed on pause/disable once the task reference exists. Existing alignment and immutable image capture remain in place.

## Pending before acceptance

1. Reproduce and resolve the original HouseScene recording click defect with runtime initialization/input/ray evidence. Check all chapter entries after Editor unlock.
2. Validate development vs sequential progression, returning from ParkScene, save failures and restarting Play. The separate roll/PCM files remain shared between Editor chapter-order modes within the selected Editor save root; order checkpoints alone are mode-separated.
3. Play through all three tape commissions; verify holds, independent A/B timing, no automatic noise skips, real replay, cancellation and source-change recovery. Replace temporary commission audio and its fixed synthetic-noise construction with authored narrative material before content acceptance.
4. Author/review reachable park reference anchors, companion animation and face shielding. Validate settings recovery, all three captures, frame immutability and hand visibility.
5. Provide physical drying/contact-print/tray props and connect actions; current blank-print color feedback is a placeholder, not a progressive image-reveal shader. Durations are short whitebox values, not real chemistry timing.
6. Compile and exercise new/old/corrupt save cases, input handoff, every processing exit and failed storage writes. No runtime pass is claimed.
7. Complete independent first-person-arm integration and its visual checks. No arm rig or animation code was changed here.
