# Park natural color tuning — 2026-11-07

User requested Beautify color tuning first, followed by small Bloom brightness/color adjustments. Target: natural foliage, restrained warm sunlight inspired by reference 3. This pass changes post processing only; it does not recreate the reference's canopy, indirect light or leaf translucency.

## Persisted parameters

| Parameter | Previous | Final |
|---|---:|---:|
| Beautify ACES pre-exposure | .85 | .9 |
| Vibrance (saturate) | .25 | .05 |
| Sharpen / clamp | .5 / .15 | .3 / .1 |
| LUT | Off | ParkNaturalSunlightLUT, .8 blend |
| Amplify walking / viewfinder intensity | .65 / 1.05 | .58 / .95 |
| Lens flare / glare / dirt | 3 / .55 / .15 | 2.1 / .5 / .1 |

Contrast and brightness remain 1, tint neutral, dither off. Beautify's duplicate bloom/lens, auto exposure and DOF remain disabled. Rim sun/shade 60/3 and all camera raising/capture control code are untouched.

LUT: 32 cubed color samples packed 1024x32 (R within a slice, B horizontal slices, G vertical). Linear data import, no compression or mipmaps, bilinear/clamp. Selective foliage mask softly spans hue .12–.48 and scales by saturation. It reduces green saturation by 10–14% at full LUT, shifts darker greens up to .012 in hue, mildly warms highlights and cools shadows, and applies a very small black-preserving toe lift. Actual scene blend is .8. Black and white endpoints remain exactly 0 and 255. Color data is generated mathematically, not sampled from the reference photo. Generator is in output/ParkNaturalGrade-2026-11-07/create-lut.cs.txt.

Lens flare gradient becomes less saturated amber / pale blue / muted violet / warm white: (1,.85,.64), (.64,.77,1), (.76,.7,.9), (1,.9,.73). Amplify regenerates its transient gradient texture at initialization. Bloom threshold, mip layout, temporal filtering and source geometry remain unchanged.

## Assets and serialization

- Assets/Art/Environments/Park/SolarBloom/ParkClearDayBeautify.asset updated, GUID retained.
- Assets/Art/Environments/Park/SolarBloom/ParkNaturalSunlightLUT.png and Unity-generated meta added.
- Assets/Scenes/ParkScene.unity: exactly 22 replaced lines in the existing Beautify, AmplifyBloom and ParkCinematicBloom component blocks, compared with Backups/ParkNaturalGrade-2026-11-07/ParkScene.unity. No other scene objects/settings touched.
- Scene written as a targeted atomic replacement after closing only Park; no broad Editor save. Final reload confirmed the values and profile synchronization. Original MainMenu editor scene restored afterward. Other chats' source/scene changes are preserved.

## Validation

- Final native 1280x720 before/after pairs under output/ParkNaturalGrade-2026-11-07/: entrance, sun-walking, sun-viewfinder. Final pairs were recaptured from the reloaded scene; baseline parameters were temporarily applied with profile sync disabled and restored in finally. Temporary Editor culling mask isolates Park from other open scenes. Rim pose is updated for each capture to match the temporary viewer position. Camera pose/FOV, culling masks, focus state and render targets restored.
- Intermediate candidate images are exploratory; only final before/after files are acceptance evidence. Temporary component-only settings were found to have reloaded from the old synchronized profile during iteration; final parameters were explicitly saved to the profile and confirmed after scene reload.
- ParkCinematicBloomChecks 12/12; repeat capture difference 0/255.
- ParkSolarBloomChecks 11/11; HDR sun peak 16.65625.
- ParkSunRimChecks 10/10.
- Six composed GPU checks with final LUT enabled: exposure remains effective (sample 210 vs 138 underexposed), tone-map HDR levels 247 vs 255, repeat capture difference 0/255, lens/history capture suppression, full-black raise occlusion, and raised lens strength above walking all passed.
- Independent review confirmed LUT packing/import/endpoints, matching profile/scene settings, exactly three changed component blocks, no duplicate effects, and preserved capture/rim behavior; no blockers.
- Final Console returned no errors. One warning appeared in concurrently modified ParkPhotoPrototype.Controls.cs: CS0414 for unused `dwell`; this task did not edit that file.

## Limits

Validation is Edit Mode/offscreen GPU rendering, not a complete Play Mode/save-flow run, build or GPU benchmark. ParkPhotoFocus does not run automatically in Edit Mode; its real callbacks were explicitly exercised in GPU integration checks. The sunlight/leaf material and scene layout remain the existing scene, so this is a restrained color/optics adjustment rather than a complete match to the reference image.
