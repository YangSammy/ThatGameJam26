# Park Beautify integration — 2026-11-07

## Saved result

- `Assets/Scenes/ParkScene.unity`: added Beautify last on Camera_Player, after ParkPhotoFocus and ParkCinematicBloom. Exactly 245 inserted lines, zero deleted lines compared with `Backups/ParkBeautify-2026-11-07/ParkScene.unity` (component plus camera-list reference).
- `Assets/Art/Environments/Park/SolarBloom/ParkClearDayBeautify.asset` and generated meta: reusable synchronized custom profile. Built-in Best Quality, full resolution, no supersampling.
- ACES; pre-exposure .85; post-brightness 1; vibrance .25; contrast 1; brightness 1; neutral tint. Sharpen .5, clamp .15, relaxation .1, motion sensitivity .8, restore speed .3. Dither off to preserve clean low-light/black transitions. Antialias fields retain package defaults, but the package's optional edge-AA shader define is disabled; no new AA system is claimed.
- Beautify Bloom, sun flares, anamorphic flares, dirt, DOF, eye adaptation, Purkinje, vignette, chromatic aberration, blur, LUT, outline and overlays are off.
- Existing rim sun/shade 60/3, Amplify walking/viewfinder .65/1.05, flare 3, glare .55 and dirt .15 are unchanged. No project settings, sky/light/material, package source, gameplay or save-format changes in this task.

## Evidence

- Native 1280x720 camera renders inspected: `output/ParkBeautify-2026-11-07/before-entrance.png`, `after-entrance.png`, `before-sun-walking.png`, `after-sun-walking.png`, `before-sun-viewfinder.png`, `after-sun-viewfinder.png`. Final pairs were recaptured on the same freshly reloaded scene, changing only Beautify enable state. ACES/AGX candidate images are exploratory, not final evidence.
- Rendering temporarily isolates Park with an Editor scene culling mask and makes it the active scene for RenderSettings. Camera pose/FOV, culling masks, render targets, focus flags and active scene are restored. `render.cs.txt` contains the executed helper method body.
- ParkCinematicBloomChecks: 12/12, repeated capture delta 0/255.
- ParkSolarBloomChecks: 11/11, sun HDR peak 16.65625.
- ParkSunRimChecks: 10/10.
- `check-chain.cs.txt`: 6/6 GPU checks using the actual camera's focus, Amplify driver and Beautify callbacks. Exposure remains effective (sample brightness 214 vs 145 underexposed), capture suppresses flare/history, repeated capture difference 0/255, full camera-raising occlusion stays black, raised-camera optics remain stronger than walking. Isolated tone-curve HDR levels 1.5 and 8 map to 246 and 255. Initial screen-filling HDR test saturated adjacent bands through Bloom; isolated the tone-curve assertion rather than changing production Bloom to suit the synthetic input.
- Final scene reload: valid profile, ACES and vibrance .25 persist; zero missing scripts; camera original position (-15,1.74,-12), rotation (0,35,0), FOV65, override mask0. Console returned zero errors/warnings. Independent source/serialization review found no blockers.
- Broad incidental serialization from Editor rendering/checks was excluded. `AfterEditorSave.unity` is retained only as a backup of that intermediate save, not used as final scene. Only the target scene and new profile were persisted; original MainMenu scene was not saved.

## Scope of validation

Edit-mode/offscreen GPU validation and serialization reload, not a standalone build or full Play Mode photography/save-flow test. Edit-mode screenshots demonstrate lighting, Bloom and Beautify; native ParkPhotoFocus does not run automatically in Edit Mode, so focus/exposure/occlusion were validated by invoking its real rendering callback in the composed GPU checks. Camera motion sharpening attenuation is configured but was not visually evaluated in a full gameplay session. No GPU performance benchmark or complete reference-image match is claimed. Cloud shapes, foliage materials and real light scattering remain the existing scene assets.

Tune on Camera_Player > Beautify or the ParkClearDayBeautify profile. Keep one owner per optical effect. Switching Beautify's tonemapper to ACES resets its vibrance/contrast, so reapply these values after changing operator; do not switch operators during photography.
