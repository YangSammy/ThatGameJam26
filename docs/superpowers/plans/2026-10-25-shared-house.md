# Shared house implementation plan

**Goal:** One playable house with five nearby clickable stations, camera transitions, Escape exit and in-session progress retention.
**Architecture:** Extend WBMain with an opt-in shared-house mode. Keep original Whitebox and TapeScene intact; create HouseScene from the authored tape scene. Tape uses its existing scene objects/controller. Other chapters are constructed once at house-local locations by existing generators, remain visible and only the selected chapter advances. Writing retains its separate layered memory view.
**Tech Stack:** Unity 2022.3, existing C# built-in renderer and input.
**Spec:** User-approved shared-house design in this conversation; click enters, Escape exits, right-click remains local back, H help.

- [x] Add shared spatial layout, remove per-chapter room shells in shared mode, retain correct world origins and navigation bounds.
- [x] Integrate authored tape controller with exclusive input, camera ownership, suspension and progress retention.
- [x] Add shared entry/exit transitions and held-input release barrier. Preserve photo mobility, writing cameras and painting cleanup.
- [x] Create HouseScene without overwriting existing scenes; save references and mark as primary build scene.
- [x] Verify all five entry/exit paths, distance gating, cancelled transitions, repeated entries, state retention, tape input and camera restoration. Run original regression checks and inspect actual Game view.

Constraints: No story additions. No new packages. No cross-session save promised. No runtime duplication of authored tape props. Remaining four chapters retain runtime geometry for this integration pass. Scene geometry migration for those chapters is a separate task.

Validation: 72 shared-house PlayMode checks; 42 tape gameplay and 28 tape close-view checks; 65 original gameplay checks. Six camera renders inspected. Final HouseScene saved, Play Mode stopped, Console 0 errors/warnings. Runtime script reload requires restarting Play Mode; transient readiness is not serialized across reload.

