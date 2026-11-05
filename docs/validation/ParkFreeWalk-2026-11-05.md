# Park free walking, pause and regional metering

User-approved scope: restore v3 WASD free walking; separate manual pause, focus suspension and completed-film return; introduce regional incident metering; clarify loading; retain existing hands, assets, route anchors and film schema.

Implementation decisions:
- Player motion uses existing CharacterController and tuning, without automatic route steering or photo-area ellipse. Companion follows existing waypoints, waits for loading and camera lowering, retains distance wait and personal space.
- Focus loss freezes controls and cancels transient drag without entering manual pause or changing stable loading/mode. Focus return arms gesture release; locomotion keys do not keep Park release gates locked. Manual pause survives focus changes. Completed-film UI is separately labelled.
- Authored non-colliding lighting volumes, blended spatial EV100. Incident meter samples player position; image exposure samples subject position. Default f/8 advice, all existing ISO/equivalent combinations retained. Directional-light intensity scales calibrated EV. This is calibrated lighting simulation, not pixel/physical metering.
- Existing arm rig remains visible/resting through input handoffs; no second model.

Execution:
1. Preserve baseline; implement movement/pause/loading and hands.
2. Implement meter volumes and bind them in ParkScene without moving existing objects.
3. Compile and run isolated focused tests for pause, movement, zones, loading, three tasks, saved reentry and hands.
4. Restore original Editor state/saves; record evidence and manual checks. Scripted Play checks are not native mouse acceptance.

## Implemented and verified

- Unity Editor compiled without errors/warnings in final Console query; Console was not cleared.
- Fresh Park Play: unpaused, zero exposures, companion waits for preparation, authored CharacterController permits movement. Park-only release gate ignores WASD; other chapters retain original release behavior.
- 13 startup/pause/input/rig assertions and 30 region/exposure checks passed (state-meter-checks.txt and final-meter-checks.txt). Focus and OS suspension were invoked through real callbacks; these checks are scripted, not native Alt-Tab input.
- 40 existing loading ray-route checks passed across ISO100/200/400/800: actual instrument hit geometry, film/leader drops, cancellation, tension, back and advance. Not native mouse acceptance.
- Existing controller in real Park Play rendered and durably saved entrance, bridge and meadow. Automatic fixture placed camera at authored references and invoked the normal shutter pipeline; all three accepted, undeveloped images remain hidden. Grass hand response consumed zero frames. Exposures at ISO800 f/8 used regional EV12.5/13/13.5.
- First/second capture retained departure hold until lowering. Restoring current task from saved roll picked next task; full scene reload retained all 3 photos and FilmComplete return state.
- A second isolated run loaded ISO200 at stable step4, then actually reloaded ParkScene: retained threaded film, ISO200 and 0 exposures; unpaused, girl waits for remaining preparation.
- Existing arm rig observed in Game-view screenshots for rest, camera body, meter, manual pause/resume and joined hands. Added missing meter GripAnchor presentation binding; no new arm model. Finger contact polish remains an art refinement.
- Scene comparison against task baseline: zero removed objects; existing position/rotation/scale unchanged; additions are 4 light volumes plus sampler and reference. Existing prefab changes from Unity save are whitespace only.

## Inspector configuration

ParkScene / Systems / RegionalMetering: ParkRegionalMetering uses open EV13 and the existing Light_Sun intensity (calibration 1.28). Child ParkLightZone objects are authored, do not create colliders:
- Entrance_Shade: EV12.5
- Path_TreeShade: EV12
- Bridge_Open: EV13
- Meadow_Sun: EV13.5

Zones blend over 1.5 local metres. Values are an initial visual calibration; no pixel luminance, automatic tree-shadow ray tracing or physically accurate light transport is claimed. Moving within the same zone will not change advice unless crossing the blended edge or changing the sun intensity. Player-position incident reading and subject-position image exposure may differ across a lighting boundary; measure near the subject. All four ISO f/8 suggestions satisfy current tolerance in these authored zones. Camera controls retain equivalent aperture/shutter choices.

## Superseded contracts

2026-11-05 guided-walk route/ellipse checks describe the earlier approved automatic-follow design. They are not acceptance criteria for this revision. The old reflection-only visual fixture was updated for PauseReason; input/property names retained where needed for existing tool compatibility. New acceptance entry: ParkFreeWalkChecks. Movement itself never reads the legacy automatic path or ellipse.

## Manual acceptance (still required for native input)

1. Enter from summer bedroom with a fresh film: no pause panel; WASD works before loading. Girl waits. E opens camera; hint explains loading.
2. Drag ISO film and leader. Switch focus during a drag; return with all mouse buttons released. No automatic completion; completed stable loading step remains. Esc twice as needed leaves instrument then opens intentional pause; Resume returns control.
3. Complete loading and lower camera. Girl starts; player is not automatically moved. Walk away: girl waits at distance. Rejoin; follow to entrance. At each point WASD is not ellipse-clamped.
4. Compare meter in entrance shade, shaded path, bridge and meadow. Check ISO100/200/400/800 advice, f/8 and an equivalent aperture/shutter pair. No automatic camera setting changes. Walk across edges: no instantaneous jump in EV.
5. Entrance: align reference/focus/exposure, shoot; lower camera before girl continues. Bridge: align, wait for turn, shoot, lower. Meadow: align, click offered hand, verify no frame used, then shoot. Third exposure opens a completed-roll return panel, not generic Paused.
6. Exit/reenter during partial loading and after first/second picture: stable step/ISO/frame count and next task stay correct. No duplicated photos. Manual pause survives Alt-Tab; ordinary loss of focus does not create manual pause on return.
7. Check arms at rest, holding camera/meter, after pause and joining hands. Meter values are also shown as a compact numeric line for small Game windows.

## Evidence and rollback

Evidence: output/QA/ParkFreeWalk_20261105 (checks, Game screenshots, preserved original files and original EditorPlaySession).
No package/build or native mouse full-run performed. Free roaming opens more viewpoints than guided walking; full park boundary/occlusion/face-concealment sweep remains manual.
Revert only the files changed by this task using its before/ snapshot and remove the newly authored meter components/objects, not unrelated pending Git changes. Formal save format and public save slot unchanged.


## Final restoration

Stopped Play, restored original HouseScene edit state (clean) and original null play-start override. Restored original EditorPlaySession with all 15 original file hashes matching. Formal saves untouched. Final Console query: 0 warning/error entries (not cleared).

Primary modified files: ParkPhotoPrototype.cs / Input / GuidedWalk / WalkSafety / Bridge / Controls / Assets / InstrumentPresentation / Sfx; FirstPersonHands.Skeletal.cs; PlayerInputPolicy.cs and PlayerInputContext.cs (default behavior unchanged outside Park); PhotographyMeterDisplay.cs; ParkScene.unity.
New runtime files: ParkLightZone.cs, ParkRegionalMetering.cs, ParkPhotoPrototype.Metering.cs. New isolated checks: Assets/Editor/Validation/ParkFreeWalkChecks.cs. Two existing reflection fixture references updated for PauseReason.
