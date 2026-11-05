# Autumn Winter Restoration Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development for isolated subsystem implementation and review; root integrates shared changes and validation.

**Goal:** Implement the user-selected autumn/winter v3 restoration while retaining the image/melt/live-paper ward renderer.

**Architecture:** Existing WritingChapter and PaintingChapter remain authoritative. Presentation uses existing partial worlds and reusable art; saves migrate rather than reset. Shared audio remains GameScore/StorySfx, no new framework.

**Tech Stack:** Unity 2022.3.62f3, Built-in, legacy input, existing Editor validation methods.

**Spec:** docs/validation/AutumnWinterV3Audit-2026-11-05.md plus user's 2026-11-05 implementation request. Current request chooses reveal driving/order/face protection/persistent paper/save/audio, autumn timing/items/failure/guidance/people/space (no deliberate autumn face concealment), winter observation/discovery/actor/pendant door/audio delivery. Ward pose replacement, global text removal and new visible 3D ward are not in this selected batch.

## Global Constraints

- Reuse current checkout and preserve concurrent uncommitted work; no reset, broad scene save, commit or push.
- No real mouse automation; use compile, deterministic state tests and offscreen checks; report manual acceptance separately.
- Keep current ward picture unless explicitly replacing posture in a future task. Reveal order must be calibrated to this image, document posture limitation.
- Autumn face concealment is explicitly not required by this user.
- Do not pretend mastered mixes are original instrument stems. Deliver reusable audio work possible locally and identify remaining composition source requirements.
- Existing v2-v4 winter checkpoints remain loadable. Restored reveal never moves backwards or completes without forward travel.

## Review Focus

- v4 time-progress saves with actor at zero: migrate without rewinding picture or auto-completing; permit forward continuation.
- Pause/exit/focus cancellation during key insertion and free look: no stuck gesture or duplicate sound.
- Held movement/reopen book: no energy injection or resumed cursor jump.
- Autumn retry/restore: vase, carry, missing-object cue and fade reset consistently.
- Repeated world lifecycle: release materials/audio/input state, avoid invisible collider blockers.

## Task 1 — Ward (root)

Files: PaintingChapter.cs, PaintingWorld3D.Ward.cs, WardImageReveal.cs, WardSoftReveal.shader, focused Editor checks.

- [x] Add behavior checks: stationary no progress; forward reveal; backtracking preserves reveal; v4 migration and v5 roundtrip; distance completion once.
- [x] Restore distance-driven WardProgress, version 5 saves and v4 progress-preserving migration; keep WardTime legacy diagnostic only.
- [x] Constrain noise to ordered image regions; preserve face opaque mask, foreground/live ink and monotonicity.
- [x] Run compile and deterministic/GPU checks, record exact evidence.

## Task 2 — Autumn presentation

Files owned: Assets/Scripts/Chapters/Writing/**, optional dedicated Writing validation and authoring files.

- [x] Frame mechanical timer with paper; preserve readable writing and bin transition.
- [x] Independent vase remains after flower pickup; distinct pickup/drag audio; local state restored.
- [x] Timeout fades memory separately; once-per-draft missing-item gesture; replace answer rings with natural cues.
- [x] Reuse real existing art for player and furniture/props. No deliberate autumn face masking.
- [x] Validate timing logic unchanged, timer framing bounds, retry/restore/gesture behavior.

## Task 3 — Winter observation and performance

Files owned: PaintingWorld3D.cs, .Pointer.cs, .Visuals.cs, .FirstGap.cs, .Sfx.cs, new .Performance.cs, PaintingVisualAssets.cs if needed; do NOT edit PaintingChapter.cs or .Ward.cs.

- [x] Direct mouse look without requiring RMB; retain edge gestures and focus aiming, no camera translation.
- [x] Once-only stalled-door pendant cue distinct from discovery confirmation.
- [x] Reuse PF_AlbumActor_E02 and authored poses, upright boarding/push/jump/walk.
- [x] Animate key/hand insertion, unlocking and door opening after accepted drop; pause route until presentation complete; restore final state without replay.
- [x] Validate cancel/restore/unique key/no wrong-drop sound, input limits.

## Task 4 — Audio delivery

Files owned: GameScore*.cs, new audio configuration/assets/tools and audio docs; do NOT edit chapter worlds or existing shared StorySfx.cs without coordination.

- [x] Inventory actual available source audio/stems and reusable Foley.
- [x] Implement real independently controllable audio layers where available, shared start scheduling, winter phase/ward progress and immediate cut; retain paper SFX independence.
- [x] Provide autumn suitcase drag and envelope cue IDs/assets to Task 2, winter hint to Task 3; preserve existing IDs.
- [x] Validate format/loop/tail/metadata; document original-stem limitations without fabricating separated instruments.

## Integration and review

- [x] Compile runtime and Editor assemblies, run model/presentation checks without native mouse play.
- [x] Independent review of changed files against selected scope; fix material findings.
- [x] Update delivery report with completed/remaining and manual acceptance tasks.

## Ledger

- Plan created. Existing dirty checkout is authoritative; isolating from HEAD would drop active work. Scoped backups and file ownership used instead.

- Implementation integrated in current dirty checkout; independent review found no confirmed P1/P2 issue. Final shoulder/hair cue added and checked. No production scene saves or commits.
- Fresh validation: Ward rules 9 groups; shader 41 frames/3 aspects; narrative 12 pass/0 fail; autumn model/presentation/timer/SFX; actual desk framing minimum paper width .613; winter interactions 4 groups; audio Unity and 36-WAV standalone validation all passed.
- Offscreen actor snapshot scaling fixed in validation tooling; live-skin comparison and final autumn recapture reviewed. Final Editor console: 0 errors/0 warnings. No native mouse tests, full Play-through, standalone build or listening certification.
- Delivery and manual checklist: docs/validation/AutumnWinterRestoration-2026-11-05.md. Original art/stem limitations remain explicit.
