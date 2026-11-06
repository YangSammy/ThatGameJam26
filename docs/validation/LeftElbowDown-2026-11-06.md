# Left handhold elbow and forearm clearance

## Scope

- Handhold-only elbow direction now points down, slightly inward and back in camera space. Desk gestures keep their existing elbow hints.
- Player wrist/forearm clearance now considers a tapered approximation of the girl's forearm, in addition to existing palm/finger protection. Only the player's display rig yields, within the existing correction budget.
- Girl animation and transform logic remain unchanged. `ParkGirlPresentation.cs` matches the pre-gesture backup byte-for-byte. No scene, layout, gameplay or save edits in this iteration.

## Files

- `Assets/Scripts/Player/FirstPersonArmRig.cs`: optional elbow direction per pose, blended in and reset with pose state.
- `Assets/Scripts/Player/FirstPersonHands.Handhold.cs`: handhold elbow direction.
- `Assets/Scripts/Player/FirstPersonHands.HandClearance.cs`: tapered forearm separation check.
- `Assets/Editor/Player/LeftHandGestureChecks.cs`: elbow alignment and forearm separation assertions; isolated preview output.

## Verification

Unity isolated real-model gesture checks passed for front and authored meadow-relative camera/contact geometry. Elbow downward alignment: 0.994 / 0.998. Held forearm envelope clearance: 0.0951 m. These are approximate bone envelopes, not mesh surface measurements. Existing finger contact, left-only display, pause, release, cancel, re-entry and unchanged fixture checkpoint assertions passed.

`FirstPersonArmChecks.Run()` passed the existing rig regression. Console error query returned zero entries; Console was not cleared.

Evidence: `output/QA/LeftElbowDown_20261106/front/` and `meadow/`, including `checks.txt` and gesture frames. These are isolated Unity pose renders, not live ParkScene mouse testing. No real mouse test was performed.

## Manual checks

1. At the meadow, click the offered hand. Left elbow should hang down rather than flare out; girl's reach should remain unchanged.
2. Watch open reach, contact and grip for wrist, finger or forearm intersection. Repeat from allowed nearby positions.
3. Release, raise the meter, pause/resume and re-enter the camera. No stranded arm or right arm should appear.
4. Check a desk activity: the handhold-specific elbow direction should not carry into desk poses.

## Limits and rollback

Clearance uses approximate envelopes and a bounded player-only correction, not complete mesh collision. Arbitrary camera positions and all animated intersections are not proven clear. Backups for this iteration are under `output/QA/LeftElbowDown_20261106/before/`; restore only relevant hunks when other work has subsequently changed these files.
