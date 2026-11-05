# Independent Work Tapes Implementation Plan

Goal: execute the user-approved seven-step flow with five physical work tapes and three unique archived copies.
Spec: ../specs/2026-11-05-independent-work-tapes-design.md
Architecture: extend RadioTapeChapter item state, HouseRadioDeck partials, existing authored Spring scene; Unity2022.3 legacy input and existing PCM persistence.

## Constraints
- Current checkout and existing modifications retained; no commit/push/new Unity checkout.
- No source PCM/hash or recorded artifact format rewrite. Existing approved toggle controls retained.
- Failed chapter commit must not archive a tape or advance the commission.
- Each tape keeps preparation independently; archived ownership supersedes loose OperationArea placements.

## Tasks / ledger
- [x] Model + migration: five candidates; FinishedCommission(index), IsArchived(index), RecordingTapeIndex binding; version9 item checkpoint; guarded transactional PrepareSubmission. Model regression checks and old-format migration.
- [x] Runtime: expand bindings; archive actual tape at authored finished anchors; hide decorative copies; prevent archived selection/preparation/record; bind recorded attempt to tape; retain preparation on retries/submission/reload.
- [x] Authoring: clone two existing good tape instances with unique IDs and all preparation/inspection/visual references; eight desk landings, five slot positions, three finished anchors; preserve scene baseline backup; rerunnable.
- [x] Verification: compile; model/acceptance checks; scene bindings and actual chapter submission/reset/restore probes; review touched diff; report native-input limits honestly.

Evidence: ../../validation/IndependentWorkTapes-2026-11-05.md. Review found and implementation fixed checkpoint restoration after peeling a seal on a partially recorded tape, including legacy recordings.

Review focus: failed commit; old saves after 0/1/2/3 completions; swapping B with recorded data; last submission handoff and success modal; stale area placements cannot resurrect archived tapes.

Ruling: explicit instruction to design and start implementation authorizes proceeding with the concrete flow discussed; no repeated design approval pause. Independent model and scene-authoring code work delegated per subagent workflow; only primary drives live Editor and scene mutation.
