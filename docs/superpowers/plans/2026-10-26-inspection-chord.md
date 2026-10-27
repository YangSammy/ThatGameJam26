# Inspection chord implementation plan

**Goal:** Implement the user's approved two-button rotation, left-button part interaction, no Tab mode, and full inspection reset.
**Architecture:** Keep SafeInspection as the only viewer. Own each pointer gesture until release; retain authored display models and original gameplay isolation. SafeInspectionPart owns temporary highlight and reset animation.
**Tech stack:** Unity 2022.3, existing IMGUI / legacy input, C#.
**Spec:** Approved design in this conversation: E opens; both mouse buttons rotate portable objects; left operates parts; right never exits; Esc exits. An approximately 80 ms chord window suppresses accidental part activation. Reset restores entry pose, distance and captured display-part states only. Fixed devices remain fixed. Hover gives subtle local feedback; UI captures its own input.

- [x] Add a failing Play Mode input regression using the existing PointerEvent boundary.
- [x] Replace mode routing in Assets/Scripts/SafeInspection.cs with gesture ownership, chord timing, reset UI and hover feedback; remove Tab.
- [x] Extend Assets/Scripts/SafeInspectionPart.cs to highlight without material mutation and smoothly reset to Capture baseline.
- [x] Update Assets/Editor/SafeInspectionChecks.cs and add InspectionGestureChecks.cs for both chord orders, release order, committed drags, UI, reset, fixed devices and source preservation.
- [x] Compile and run in HouseScene and TapeScene; verify original safe-inspection checks, tape gameplay, and return poses as relevant.
- [x] Update README and QA evidence, review scoped changes, restore HouseScene edit mode.

Constraints: no Computer Use; no new packages; no scene rebuild; no save reset. Preserve backups under QA/Backups/before-inspection-chord. Existing project is not a Git checkout.


Validation ledger
- Before implementation, right-button regression failed because it closed inspection.
- Review found delayed ray repick could miss a moving display part. Added failing regression (18 checks completed before failure); now capture part at MouseDown without activating it, then commit after chord grace.
- Final gesture checks: HouseScene 30, TapeScene 30 passed. Existing safe inspection checks: HouseScene 766, TapeScene 507 (removed obsolete mode-only assertions, retained exterior/structure/source restoration coverage).
- TapeScene: 20 operating-view/input checks and 42 full gameplay checks passed. House return pose: 50 passed.
- Unity compilation and final Console query: no errors or warnings. Rendered Game screenshot reviewed: QA/inspection-chord-house.png.
- A separate read-only reviewer found no further issues after the moving-target correction.
- Ruling: reset is the existing ResetView API extended to full display reset, so R and the left UI button share one path. Capture baseline is retained; no game model or save mutations.
- Validation uses Unity MCP input-handler calls and engine screenshot capture; no Computer Use, physical input automation or executable build validation.
- No scene/prefab modifications; final state HouseScene edit mode. Backups in QA/Backups/before-inspection-chord.
