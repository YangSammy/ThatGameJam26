# Safe Inspection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add reversible visual and structural inspection without touching chapter progress.
**Architecture:** Authored subjects pair source transforms with display-only models. One camera-owned SafeInspection handles modal input, rendering, parts and cleanup; existing house/tape owners yield input while it is active.
**Tech Stack:** Unity 2022.3, C#, existing legacy mouse/keyboard input, editor validation menus.
**Spec:** docs/superpowers/specs/2026-10-26-safe-inspection-design.md

## Global Constraints
- No inspection model creation during play; no gameplay components on display copies.
- No changes to save formats, build settings or standalone InkRoad scene.
- Native execution in the current Unity project (no Git repository exists); exact scene/script backups in QA/Backups/before-safe-inspection.

## Review Focus
- Escape/held mouse events leaking into gameplay during open or close.
- Focus loss or disabled owner leaving source hidden or cursor free.
- Model bounds and viewing angle clipping long/thin objects.
- Loaded cassette/source visual pose sync, rather than resetting gameplay state.
- Fixed device camera and ordinary overlay restoration, including nested tape close views.

## Task 1: Authored safe inspection and scene setup
- [x] Add presence check and observe failure before implementation.
- [x] Create SafeInspectionSubject.cs (source/display pose mappings), SafeInspectionPart.cs (bounded joints), SafeInspection.cs (modal controller).
- [x] Create SafeInspectionBuild.cs: populate authored models, parts, details and overlay camera in HouseScene and TapeScene without rebuilding room.
- [x] Add E entry in WBMain.SharedHouse.cs and TapeSceneController.cs; yield input/camera while active and during release gate. Preserve existing left-click entry.
- [x] Run Unity compilation and edit scene reference checks.

## Task 2: Integration and recovery validation
- [x] SafeInspectionChecks.Run in Play Mode: open each subject, rotate/flip/zoom limits, operate every part, close/cancel/disable/focus restoration, compare source poses and model snapshots, repeated opening object counts, depth/occlusion picking.
- [x] Verify tape drag/play and room return regressions, inspect screenshots of small item and fixed device.
- [x] Fresh reviewer checks changed files; fix material findings and rerun relevant checks.
- [x] Document controls/results/limits in README and QA report; leave HouseScene ready to play.

## Ledger
- Ruling: user explicitly said start implementation after approving the approach; proceed without repeating design approval. New key E avoids changing existing chapter entry semantics.
- Baseline: HouseScene opened clean in edit mode; Console 0 errors/warnings. No Git repository; backups used instead of worktree/commits.

- Task 1 complete: presence check failed before implementation; authored scene installation compiled successfully.
- Task 2 complete: House 776 and Tape 511 inspection assertions passed; existing house 72, return 50, tape 28 passed. Desktop E/F/Tab/drag/Esc verified; exposed-photo model and isolated save bytes preserved.
- Review fixes: held gameplay gesture entry rejected; disabled owner releases input; any held key blocks release; deck includes cassette displays; material property colors synchronized. Fast mouse event delivery and invisible shell picking also covered.
- Ruling: preserve left-click gameplay and expose safe inspection with E; close-up whitebox art remains provisional. Open covers/pages receive extra camera clearance. See QA/safe-inspection-validation.md for scope and limits.
