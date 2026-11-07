# Park Beautify Integration Plan

> Execute inline with superpowers:executing-plans. User approved the division of responsibilities in this chat.

**Goal:** Clearer park color and highlight detail while preserving sun-aware rim light, raised-camera optics, and photography controls.
**Architecture:** Add built-in Beautify after ParkCinematicBloom on Camera_Player. Use a reusable park profile for fixed tonemapping, restrained vibrance and sharpening. Keep Amplify as the only bloom/lens owner and ParkPhotoFocus as exposure/focus owner.
**Tech Stack:** Unity 2022.3.62f3, Built-in/Gamma, Beautify built-in 22.0.2, Amplify Bloom.
**Spec:** User-approved proposal in this chat: Beautify grades, Amplify blooms; existing rim and photography remain.

## Constraints and review focus
- Work in the existing Unity checkout; preserve other concurrent edits and open scenes.
- No pipeline/color-space changes, package source modifications or gameplay changes.
- No automatic exposure, extra DOF, duplicate sun flare/bloom or dynamic low-light recoloring.
- Keep capture history suppression and black camera-raising occlusion.
- Check rendered highlights, shaded foliage, repeated capture, camera component order and shader errors.

## Task: Configure and validate the park finish
- [x] Back up ParkScene; record baseline components and native before images.
- [x] Add Beautify last, configure a park profile, compare ACES/AGX and select a restrained finish from native renders.
- [x] Save only ParkScene and the new profile. Inspect delta against the pre-task backup.
- [x] Run existing rim, solar and cinematic regression checks plus native render/capture checks; inspect screenshots and Console.
- [x] Review and record final parameters, evidence and limitations.

## Execution ledger
- Ruling: This is scene/profile configuration; use existing regression checks and real GPU render comparisons instead of adding tests that merely mirror serialized settings.
- Ruling: Keep the active checkout because the attached Unity Editor and prior uncommitted park effects live here. Do not create a disconnected worktree or commit other threads' edits.
- Selected ACES .85 pre-exposure, 1 post-brightness, .25 vibrance, contrast 1, sharpen .5/clamp .15. Explicitly set vibrance after selecting ACES because the plugin resets vibrance/contrast when switching to ACES.
- Validation helper initially restored serialized Amplify state with a null generated lens LUT; repaired the transient LUT and changed the helper to restore individual fields. This was a validation artifact, not a runtime integration bug. Final before/after images were recaptured on the reloaded final scene.
- Editor save after offscreen checks added unrelated prefab overrides/generated objects. Narrowed the serialized delta to the new Beautify component and camera component-list entry, then reloaded successfully. Final delta: 245 insertions, zero deletions against the task backup.
- 33 existing checks + 6 GPU integration checks passed. Fresh independent read-only review found no blockers. No runtime or plugin source was changed.
