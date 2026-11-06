# Natural handhold contact — 2026-11-06

User contract: preserve the girl's offered-hand animation, turn her overall response slightly to screen right, rebuild player contact from the palm, keep the wrist continuous with the forearm, conceal the arm root, and reduce the overbright skin.

Changes:
- FirstPersonHands.Handhold.cs: lower-left approach projected into the offered palm plane; palm-width offset avoids the girl's curled fingers; distributed thumb opposition replaces unconstrained tip CCD; coordinated finger curl.
- FirstPersonArmRig.cs: handhold-only contact-first arm construction, preserving segment lengths and scales. The forearm follows the palm axis; the shoulder extends below the frame. Existing desk and missing-girl fallback use the previous solve.
- FirstPersonHands.HandClearance.cs: check open finger direction as well as closing; maximum display correction 6 mm instead of 30–70 mm.
- ParkCompanion.cs: response heading yaw -12 degrees (toward image right). Girl animation and ParkGirlPresentation were not edited by this task.
- BareArms.mat: Standard material albedo tint (0.62, 0.57, 0.52), retaining texture and scene lighting. This shared material also affects desk arms.
- LeftHandGestureChecks.cs: wrist angle and shoulder framing checks replace the old elbow-down-only criterion; palm anchor limit tightened from 60 to 45 mm.

Evidence: output/QA/HandholdNatural_20261106/front and meadow, with reach/release PNGs and checks.txt. Before-files preserved under before/. No scene or save writes.

Baseline: old sequence passed its envelope checks despite 54.5 mm palm anchor separation. Tightened contact check failed before the fix. Editor initially had HouseScene running with a transition timeout; isolated checks temporarily masked global pause/transition flags in a synchronous try/finally and restored them. Editor was subsequently in Edit mode for compilation.

Validation: Unity compiled; front and authored meadow-relative 61-frame sequences passed all contact, wrist, frame, left-only, input gate, shutter, release, instrument switch, re-entry, pause, cancellation and checkpoint invariance checks. Final wrist bend 0 degrees; shoulder viewport y -0.71 / -0.41; palm anchor separation 40.4 / 43.3 mm. Existing FirstPersonArmChecks passed (52 bindings, independent fingers, unchanged bone scales, bounded normal reach, camera/fallback gestures). Visually reviewed both held renders.

Limits: these are isolated real-model renders and approximate bone envelopes, not a live ParkScene keyboard/mouse playthrough or a triangle-surface collision proof. Palm anchor separation is not visible skin gap. Existing Stage 1 timed out log predates the changes. No build made.
