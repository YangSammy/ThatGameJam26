# V3 Alignment Implementation Plan

> Execute incrementally with the existing Unity feature and test workflows. This file is the persistent implementation ledger; an unchecked item is not delivered.

**Goal:** Align the existing five-day game with the v3 specification, preserving asset identities and existing recorded audio, photographs and checkpoints.

**Spec:** `C:/Users/Chu/Downloads/留在手边的事_策划与制作需求_v3/留在手边的事_完整玩法策划案_v3.pdf`, its production spreadsheet, and the approved 2026-11-03 gap analysis in this conversation.

**Architecture:** Extend WBMain, the existing chapter models, physical interactions and visual adapters. No new gameplay framework, input package, save format or rendering pipeline. Unity 2022.3.62f3, Built-in, legacy input.

## Decisions and constraints

- The user approved starting the proposed execution sequence on 2026-11-03. V3 is the target; earlier requested deviations are documented below, not mistaken for defects.
- Work in the current `dev-Nicole` checkout connected to Unity. Preserve unrelated uncommitted changes. Do not commit, push, reset, or create a second Unity workspace.
- Preserve scene identities, source model GUIDs, existing anchors and persisted artifacts. Changes to room composition will be separately backed up and explicitly recorded when implemented.
- Keep current guidance until equivalent physical cues exist. Do not make the game unplayable by globally hiding text before replacements are ready.
- Preserve completed submissions. New validation applies at submission time; it must not erase previously completed chapters or rewrite source PCM/hashes.
- No new automatic noise detection in the recorder. Only the acceptance layer may inspect noise metadata.
- A third-party fixture or a direct API call is not native mouse/keyboard acceptance. Record those separately.
- No missing original music or formal character animation is silently replaced by a claimed final asset.

## V3 rulings (previously intentional deviations)

| Area | Existing deviation | V3 target |
|---|---|---|
| Room | Study with shared desk/curtained darkroom | One seasonal bedroom, photo-wall entry, writing area; reuse current assets |
| Guidance | English HUD/tooltips/control labels | Physical/symbolic cues; remove text only after usable replacement |
| Tape selection | All cassettes on desktop | Open storage, choose independent cassette |
| Tape shell | LMB moves, two-button chord rotates | LMB shell rotates during preparation; explicit grip handles relocation |
| Transport | Latched UI REC starts/rewinds A | A mouse controls remain independent; held B S/F/J/L and Stop |
| Photography | Must finish loading in House before Park | Photo entry leads to memory; one authoritative mechanical loading sequence |
| Completion | Remain indefinitely in completed station | Preserve results, then transition to next season with committed progress |

## Execution tasks

### Task 1: Baseline and source of truth
- [x] Capture git/Unity/Console baseline and recoverable copies before editing.
- [x] Link this plan from `docs/design.html` and project context, clearly marking the old snapshot.
- [x] Maintain this ledger with actual verification outcomes.

### Task 2: First-day tape contract
Files: `TapeCommissionCompiler.cs`, `TapeAcceptanceRules.cs`, `Assets/Data/TapeCommissions/Acceptance_*.asset`, `HouseRadioDeck.Recording.cs`, `HouseRadioDeck.DualDeck.cs`, `TapeTransportPanel.cs`, `HouseRadioDeck.Workbench.cs`, existing editor checks.
Interfaces: existing `Reject(int[])`, `ApplyRecordingKeys(bool,bool,bool,bool,bool)`, `SourceControl(int)`, tape preparation and source identity.
- [x] RED: unchanged recording rejected for each real commission; leaving either whole noise segment in commission 2 rejected; deliberate clean cuts pass.
- [x] Use tolerances that preserve forgiving boundaries while requiring each noise segment to be meaningfully skipped. Do not change source files, source hashes or saved PCM.
- [x] RED: B record/stop/rewind/play cannot start, rewind or stop A; S alone cannot move B; stopping B produces no gap; releasing S while holding F leaves an audible blank when appending.
- [x] Remove automatic A commands, unify B held inputs, release and Stop rearming. Update physical and interim UI feedback consistently. Program-driven validation passed; native held-input acceptance remains open.
- [ ] Restore selection and preparation gestures with separate relocation grip, actual axle hit targets and per-hole preparation. Preserve safe inspection as non-mutating.
- [ ] Verify real recording/replay/submission and interrupted preparation; document unavailable original music separately.

### Task 3: Photography and darkroom
Files: `WBMain.SharedHouse.cs`, `PhotoWorld3D.ParkFilm.cs`, `ParkPhotoPrototype.*`, `ParkFilmStore`, existing physical darkroom controllers.
- [x] Consolidate one full loading sequence (shaft, back, cartridge, leader, spool engagement, tension, close, advance) and photo entry. Park owns loading; House no longer writes a competing simplified state. Four-ISO collider/controller checks pass.
- [x] Resume at unfinished photography location without duplicate frames; preserve settings and interaction state. Three authored anchors; real House/Park reload tested after frame 1. Native first/third-task resume acceptance remains below.
- [ ] Verify three real captures and distinct character actions, rewind, extraction and dark-bag transfer.
- [ ] Improve non-text readability of chemical vessels, trays, valves, racks and contact frame without discarding hit fixes.
- [ ] Validate full chemistry/printing chain, held input, cancellation, save/restart and three actual images.

Task 3 verification detail: real game-camera captures for all three tasks and the complete controller/receiver darkroom chain passed, including partial rewind/bag/exposure and processing saves, actual scene reload after completion, and failed-save exit protection. Native Esc/Leave memory/Return to darkroom clicks passed. The unchecked composite items still include final character posing/face concealment, all native held/drag gestures, and the remaining physical recognition cues. See [photography/darkroom execution report](../../validation/V3PhotographyDarkroom-2026-11-03.md).

### Task 4: Autumn and winter
Files: `WritingWorld3D.*`, `WritingChapter.cs`, `PaintingWorld3D.*`, `PaintingChapter.cs`, existing D/E visual adapters.
- [ ] Autumn: item carrying/contact, E preparation feedback, spatial readability; retain dual-input timer and checkpoint rules.
- [ ] Winter: two observation environments and anchors, first-to-second transition, discoverable pendant.
- [ ] Validate existing swing/bridge/crate/wheel/high-platform/key chain with native inputs.
- [ ] Ward: formal visual/character integration, actual nose anchor, persistent paper, all-view face concealment.

### Task 5: Five days and ending
Files: `WBMain.Progress.cs`, existing house/narrative binding and ending visuals.
- [ ] Stable completion -> save -> release input -> transition -> seasonal room -> next entry.
- [ ] Preserve/display three tapes, three photos, crumpled drafts and final paper across days/restarts.
- [ ] Fifth day: view artifacts without restarting old puzzles; thaw, flower, water/birds/light and door-threshold ending.

### Task 6: Presentation and release acceptance
- [ ] Replace textual guidance chapter by chapter with progressive physical cues and optional non-solving assistance.
- [ ] Integrate available audio; explicitly inventory missing formal music, SFX and character actions.
- [ ] Check face concealment in every reachable camera/action/photo state.
- [ ] Native Windows full five-day playthrough, per-chapter cancellation/restart matrix, recorded PCM/image integrity, no debug unlocks.

## Review focus
1. Saved source versions and accepted artifacts remain usable after acceptance changes.
2. Modal/camera/inspection handoffs release both B inputs without implicitly stopping A during valid within-workspace handoffs.
3. UI and physical keys do not implement different recording rules or hidden source automation.
4. No scene/prefab changes overwrite unrelated uncommitted work or authored anchors.
5. Partial success is never recorded as whole-game or native-input certification.

## Ledger

- 2026-11-04 Task 3, Windows development player. Starting with three camera-captured frames on an ISO 400 roll, a wheel without holding the spindle left progress unchanged. Holding plus four native wheel notches saved 50%. Esc during the held gesture canceled it. A new player process restored 50%, four more notches completed rewind, and the physical back and cartridge accepted click and drag. Found and fixed the extracted cartridge displaying ISO 100 instead of the loaded 400. Repeated the sequence in the rebuilt player and checked the actual cartridge label. All three negative files and 29 original save-directory files matched the baseline. This case ended while carrying the cartridge. [Before](../../../output/QA/2026-11-04-Standalone-Rewind/wrong-iso.png) · [After](../../../output/QA/2026-11-04-Standalone-Rewind/extracted.png) · [Resume trace](../../../output/QA/2026-11-04-Standalone-Rewind/fixed2.log).

- 2026-11-04 Task 3: three saved capture assessments now agree with the authored tasks; invalid exposure no longer permits the meadow hand response. Instrument hints verified in Game view. Exposure switch press/release, actual scene reload and sustained press to PaperDeveloper passed via Unity Game View events, not a standalone hardware test. All 29 original files restored; scene files unchanged. [Execution report](../../validation/PhotographyTaskFeedback-2026-11-04.md).

- 2026-11-04 Task 3 continuation: native meadow hand click, focus wheel, exit/reentry, third shutter and return-to-darkroom passed with fixture-prepared framing. Fixed the focus shader's infinitely thin sharp plane; actual GPU comparison failed before and passed after adding the existing focus-tolerance clear band. Partial rewind (25%) and exposure (1.381429 s remaining) survive actual HouseScene reload; native short exposure/release and Esc pause/resume passed. Full held rewind/wheel and sustained exposure still require native acceptance; controller checks are not substitutes. Actual developed gallery shows all three captured frames. Old whitebox reference images, visible faces, photo-wall entry and non-text cues remain v3 gaps. Original 29 save files restored with matching hashes; scene files unchanged. See [continuation report](../../validation/V3PhotographyFocusAcceptance-2026-11-04.md). Composite Task 3 items remain open.

- 2026-11-03: User approved the proposed sequence; existing checkout selected because Unity is connected to it and contains other work. No commit/worktree operation will be used.
- Pre-flight: Task 2 preserves transport/save interfaces consumed by Task 5. Task 3 loading/return changes must keep photo artifacts available to Task 5. Task 6 textual removal depends on chapter physical cues; it must not run before them.
- Baseline: Unity MCP reports this project, Unity 2022.3.62f3, HouseScene, Edit mode, scene clean, reset-on-Editor-Play enabled. Existing worktree modifications predate this plan.
- Task 1 complete: baseline and recoverable source/save copies in `output/QA/2026-11-03-V3-Alignment`; old design snapshot linked to this contract.
- Task 2 acceptance/transport increment: RED reproduced source-copy acceptance bypass (5 failed assertions), B auto-start/rewind (2 failed assertions), and missing blank run (1 failed assertion). Current and historical-source acceptance assets now allow 0.75 s noise, with 2 s blank, 80% clean and 70% per-cue coverage retained. This is an implementation tolerance, not a numeric requirement quoted from v3. Completed chapters/PCM remain intact.
- Review: a read-only independent reviewer found three release/stop issues. Added RED tests, then fixed preparation handoff stopping B only, physical Stop latch and pointer Stop-bypass. All three pass; existing A/B independence checks also pass. Source-drag handoffs observe key release outside the recording loop without starting a new B gesture.
- Verification: acceptance 16 PASS; transport 7 PASS; controller 16 PASS; existing TapeP0FlowChecks 62 PASS; DualDeckV6Checks 48 PASS; AudioIntegrationValidation source/legacy/approved-WAV checks PASS. These are Editor/program-driven Play Mode tests, not native keyboard certification.
- Scene changes: five authored UI held-key components, four button labels, hidden obsolete combined-listen shortcut; no scene Transform edits. Screenshot is a fixture-prepared Game view, not a manual playthrough.
- Remaining Task 2: box selection, LMB preparation shell/grip separation, non-text teaching/feedback and native operation acceptance. Tasks 3–6 have not started. No missing music or character assets were fabricated.
- User steering: prioritize Task 3 before the remaining tape/presentation work. Task 3 first increment implemented and independently reviewed on 2026-11-03; earlier "not started" entry above is historical.
- Task 3 fixes: reproduced House overwriting mechanical steps, closed-back collider blocking loading controls, and default nested checkpoints being mistaken for resumed visits. Fixed all three. 6 House ownership checks, 37 loading/lifecycle contracts, 40 four-ISO pointer checks, 16 actual-capture probes passed; full darkroom controller chain and completed scene reload passed. Native long-gesture and character visual acceptance remain open.
- Task 3 preservation: no existing House/Park transform poses changed or removed; 29 original save files restored with identical SHA256. Evidence and backups: `output/QA/2026-11-03-V3-Photography-Darkroom`. No build, commit or push.
- Task 3 native-input increment: fixed Body/Meter cursor mismatch, fast mechanical drags losing press origins, and oversized old bottle hits intercepting visible bottles behind. 83 existing pointer/loading contracts, 5 buffered-gesture checks and 3 bottle-hit checks pass. Native ISO400 loading, all chemical pours/agitation/drains, negative hanging, frame assembly, paper baths/wash/hanging passed. Native partial exposure/release/Esc passed; full exposure and rewind/bag inputs still include controller assistance and are not fully native-certified.
- Task 3 recovery increment: processing session restored and continued; partial exposure time preserved; completed HouseScene reload remains Complete/Developed. Final Console warning/error query 0. Original 29 files restored byte-for-byte; both scene files match turn baseline. Detailed evidence and remaining limits: [native photography/darkroom report](../../validation/V3NativePhotographyDarkroom-2026-11-03.md). Task 3 composite acceptance remains open; no build, commit or push.

- 2026-11-04 Task 3 presentation increment: replaced three whitebox references with current formal girl at unchanged anchors; preserved reference aspect ratio and fixed NPOT distortion. Enabled/tuned existing response privacy pose while keeping neutral references separate. Native three shutters, meadow hand response, pause and House/Park recovery passed with fixture-prepared placement/settings. Original 29 saves restored with matching hashes; scene files unchanged. Foreground-hand blur, all-angle face concealment, exact lighting presentation and generic-vs-task assessment feedback remain open. See [presentation report](../../validation/V3ParkPresentation-2026-11-04.md). Composite Task 3 acceptance remains open.
