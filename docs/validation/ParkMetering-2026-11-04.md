# Park preset metering and ISO equivalence — 2026-11-04

> Follow-up: the instrument-map hot-reload issue recorded below was subsequently fixed and retested. See [Photography hot reload verification](PhotographyHotReload-2026-11-04.md). The original observations below remain as evidence of this iteration.

## Scope and result

Retained all three authored EV100 = 13 presets. No zone sampling, scene lighting changes, layout/anchor changes, save migration or exposure-gate relaxation. Unity 2022.3.62f3, Built-in, Gamma, D3D11; verified in the project's ParkScene Play Mode.

Fixed an actionable discrepancy: the meter previously recommended ideal reciprocals 1/128, 1/256, 1/512 and 1/1024, whereas the mechanical dial supports 1/125, 1/250, 1/500 and 1/1000. All three readout paths now select the available shutter with the smallest absolute EV error using the same shutter table as the camera.

| ISO | Meter recommendation at EV13 | Error |
|---|---|---|
| 100 | f/8, 1/125 s | -0.0342 EV |
| 200 | f/8, 1/250 s | -0.0342 EV |
| 400 | f/8, 1/500 s | -0.0342 EV |
| 800 | f/8, 1/1000 s | -0.0342 EV |

## Lighting assessment and its limits

The actual authored points are Entrance / Waypoint_00, Bridge / Waypoint_07_Waterside and Meadow / Waypoint_12_WaitForPlayer. They share the directional sun (intensity 1.28, warm color) and Trilight ambient setup. Checked actual rendered negatives from all three reference viewpoints:

- Entrance: shaded subject and path; bright background. The character remains substantially darker than the surroundings.
- Bridge: directly illuminated face/clothing and bridge surface; readable without broad white clipping.
- Meadow: partial shadow on the subject with lit surroundings; visible hair, clothes and hands.

EV13 is the game's reference for the existing normal render, **not an absolute lux calibration of Unity lights**. Changing a preset and then following its new recommendation would still produce approximately the same neutral render. It would not fix subject/background contrast. Accordingly, no arbitrary different EVs were assigned merely because one subject is in shadow. Brightening the entrance subject would require a separate art/exposure-bias decision; this iteration preserves its current lighting.

At f/8, four equivalent ISO/shutter captures at each point differ in average rendered luminance by less than 0.5% (minor motion/render differences). Entrance has no fully white pixels, bridge has 12–22 of 517,440, meadow has none. These checks support relative exposure consistency, not a claim of physically calibrated illumination.

## Changed files

- `Assets/Scripts/Park/ParkPhotoFocus.cs`: shared mechanical shutter values and nearest selectable recommendation.
- `Assets/Scripts/Park/ParkPhotoPrototype.Controls.cs`: camera dial and fallback meter readout use those values.
- `Assets/Scripts/Park/ParkPhotoPrototype.Assets.cs`: formal-model tooltip/readout uses the same recommendation.
- `Assets/Scripts/Chapters/Photography/PhotographyMeterDisplay.cs`: physical B04 dynamic display uses the same recommendation.
- `Assets/Editor/Validation/ParkMeteringChecks.cs` and `.meta`: isolated Editor Play verification of meter text, actual capture acceptance/rendering/storage, and undeveloped-image access protection.

Changes were compared with the snapshot taken at the beginning of this iteration, rather than with HEAD, because the checkout already contained unrelated work. The four runtime files above are the only changed files in that baseline snapshot. HouseScene and ParkScene are byte-identical to that snapshot. No save/state/configuration structure changed.

## Actual Play Mode checks

The fixture sets completed loading, ISO, existing reference poses and camera controls, then runs production task alignment, companion response, `Capture`, `Camera.Render` and `SaveExposure`. It does not replace the capture with a demo image or directly mark it accepted. This is automated Play verification, **not 18 manually walked and loaded rolls**. Full physical loading/rewinding and native mouse operation were outside this focused matrix.

All 18 supported qualifying aperture/shutter/ISO pairs were checked at all three points: **54 saved exposures across 18 rolls**. A separate enumeration of the actual camera arrays and ±0.7 EV gate confirms the matrix covers every currently qualifying pair at EV13.

| ISO | Tested aperture → shutter denominator |
|---|---|
| 100 | 2.8→1000, 4→500, 5.6→250, 8→125, 11→60, 16→30 |
| 200 | 4→1000, 5.6→500, 8→250, 11→125, 16→60 |
| 400 | 5.6→1000, 8→500, 11→250, 16→125 |
| 800 | 8→1000, 11→500, 16→250 |

Assertions passed:

- Each valid capture advances exactly one frame and reloads with correct ISO, aperture, accepted/sharp flags and exposure error. Every roll ends AwaitingDevelopment with three frames.
- Deliberately incorrect exposure is rejected before each valid shot, without consuming film: 54 checks.
- Meadow hand acceptance consumes no frame; the subsequent valid shutter does: 18 checks.
- None of the 54 undeveloped images is available through the developed-image reader.
- Four actual meter display strings recommend selectable shutter values.
- Existing exposure/focus response checks and GPU exposure darkening/brightening/neutral-preservation checks pass.
- Final Console query returned zero warning/error entries; no Console-clearing operation was used by this task.

Wide apertures at high ISO can exceed the 1/1000 s limit. For example ISO800 f/2.8 is not a valid equivalent combination at EV13. These remain correctly rejected; the tests do not make every arbitrary combination acceptable.

## Test complications and unresolved scope

- A first fixture used a fixed 300 Update calls to await the companion's turn. At high Editor frame rates this represented less than the needed response time. Corrected the test to simulate three seconds through the existing Update path; gameplay turn speed was not changed.
- Editing the test script during Play triggered a domain reload and exposed an existing nonserialized instrument-map issue (`rewind` key missing in SyncAssetInstruments). Exited and freshly restarted Play before the final matrix. This hot-reload lifecycle issue is not fixed here and remains a separate Editor-only follow-up; the 54 final captures all passed from fresh Play.

## Preservation and evidence

Returned to HouseScene in Edit Mode, scene clean, not compiling. Archived isolated test-session data under the QA directory, restored the original save backup, then compared all **29 original files by SHA-256: zero mismatches**, with 29 files present. Did not commit, push, reset the checkout or save scene edits.

Evidence directory: `output/QA/2026-11-04-Park-Metering/`

- `red-meter-scale.json`, `green-meter-scale.json`, `regression.json`
- `authored-lighting.json`, `coverage.json`, `matrix-summary.json`, `iso*-a*.json`
- `Entrance-ISO100.png`, `Bridge-ISO100.png`, `Meadow-ISO100.png`, plus ISO200/400/800 counterparts: actual rendered negative bytes copied for QA only, never exposed in the park UI.
- `image-statistics.json`, `console-before.json`, `console-after.json`
- `source-before.zip`, `iteration.patch`, `diff-summary.json`, `git-before.txt`
- `saves-before.zip`, `saves-before-hashes.json`, `save-restore.json`, `restore-editor.json`

Re-run matrix checks only in an isolated Editor Park Play session with ResetOnEditorPlay enabled. `ParkMeteringChecks.CaptureEquivalentRoll(iso, apertureIndex)` creates a test roll and repositions runtime objects; do not call it against a player's active non-isolated session.

Rollback only the four runtime-file changes shown in `iteration.patch`, preserving other work. The baseline ZIP contains the pre-iteration versions. The editor test can be removed together with its meta if no longer needed; no scene rollback or save migration is required.
