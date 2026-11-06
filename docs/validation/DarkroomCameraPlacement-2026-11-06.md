# Darkroom camera placement

## Cause and correction

Shortcut 2 correctly entered DirectDarkroom with a ready input context and an
orange outline command buffer. A fresh exposed roll selected `rewind`, but the
camera remained in the bedroom while the player was placed at `darkroomView`.
The target projected to viewport `(7.43, -0.58, 0.45)`, outside the screen.

Moved the Summer scene's existing `SummerPhotoRoom.cameraDock` from
`(-1.66, 0.80, -1.45)` to `(0.55, 0.85, 3.17)`, retaining its rotation. The existing
scene binding moves the camera entry and synchronizes the operational model,
back, rewind control, colliders and film extraction point to this same anchor.
No workflow stages are skipped. The attempted dynamic shortcut placement code
was removed; both shortcut entry implementations retain their original behavior.
Existing unrelated scene and script edits were preserved.

## Verification

- Unity 2022.3.62f3 Editor Play Mode, isolated EditorPlaySession.
- Invoked `EditorCompleteStage(2)`, the same entry used by the unmodified 2 key.
- Rewind projected to `(0.69, 0.50, 1.33)` and the player-camera ray picked `rewind`.
- Screenshot inspected: `output/QA/darkroom-camera-moved.png`; orange rewind
  border and the complete camera are visible from the original shortcut view.
- `MemoryWhitebox.Editor.DarkroomCameraPlacementChecks.Run(WBMain.Instance)`:
  34 checks passed. Covers ready input, visible/reachable rewind, orange outline
  material and attached command buffer, rewind/open/extract, adjacent bag,
  reel/tank, three pour/agitate/drain cycles, negative rinse/dry, contact exposure,
  paper developer/stop/fixer/wash/dry and completed save.
- The tank carry test routes around the dark bag using normal collision sweeps;
  its initial straight diagonal correctly failed against the bag.
- Final Console query returned no errors or warnings. Editor was back in Edit Mode.
- A subsequent completed-roll reload probe could not run because Play Mode had
  already ended; persistence reload was not verified in this run.

Raw checks: `output/QA/darkroom-camera-placement-checks.txt`.
Timers were advanced through the normal tick method during the automated flow;
this is an Editor interaction/state regression, not a packaged-build or physical
keyboard/mouse end-to-end test.
