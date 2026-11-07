# Solar Bloom reference implementation — 2026-11-07

User approved moving from broad sky brightening to a visible HDR sun, solar ghosts/halo/star rays, natural scene occlusion, stronger raised-camera optics and modest atmospheric warmth.

## Saved changes

- Dedicated `Assets/Art/Environments/Park/SolarBloom/ParkSolarSky.shader` and material retain the existing cloud pattern with lower cloud brightness. Sun angular radius .7 degrees, radiance (60,48,33); cloud density attenuates it. Half precision output preserves HDR. ParkSunSky binds direction to the existing Light_Sun; main sunlight orientation, route and character positions were retained.
- ParkCinematicBloom gates optical strength by projected sun position; behind/offscreen/disabled sun suppresses lens effects. Walking optical blend .1; viewfinder 1, with existing smooth runtime raise/lower transition.
- Bloom: threshold 1.15; High/Realistic/Half; 6 mips weighted .1/.15/.2/.2/.2/.15; walking intensity .65, viewfinder 1.05. Lens source mip 1; secondary threshold disabled after tuning to preserve the concentrated small source.
- Five warm/blue/violet ghosts, dispersal .6, intensity 3; ghost normalized intensity 1.5, factor 3, falloff 1. Halo normalized intensity .6, width .35, factor 1.5, falloff 4. SunnyCross glare .55, streak scale .5, 3 passes. Dirt .15. Bokeh and texture starburst off.
- Warmed linear fog color (.74,.75,.72), range 55–180. No volumetric ray effect added.
- Actual photographic render uses an HDR temporary target before conversion to the existing LDR output, restoring both camera and active render targets in finally. PNG format, film state and exposure logic remain unchanged. Existing capture guard excludes viewfinder lens effects and temporal history.

## Verification

- Unity script/shader compilation succeeded; no project errors in Console. MCP websocket reconnect warnings accompanied domain reloads.
- ParkSolarBloomChecks: 11/11. HDR disc measured peak 16.65625 under its test sky/cloud direction; normal sky below threshold. Opaque blocker removes disc; removing blocker restores it. Centered/offscreen/behind/disabled/missing sun and direction synchronization verified.
- ParkCinematicBloomChecks: 12/12, repeated capture difference 0/255, render targets restored.
- ParkSunRimChecks: 10/10; existing shaded-character behavior retained.
- Independent review found missing-sun radiance retention; fixed and regression checked.
- Native renders inspected at sunny waterside and tree-occluded entrance, walking and viewfinder variants. Output: `output/ParkSolarBloom-2026-11-07/`.
- Another task loaded Bedroom_Summer additively during validation. Initial mixed-scene captures were discarded/overwritten. Final screenshots temporarily assign Park a unique Editor scene culling mask and apply it to the validation camera; all masks, transform, FOV and target textures restored. No runtime culling changes added. No other scene was saved.
- Park scene saved clean; original camera (-15,1.74,-12), FOV 65, override culling mask 0. Backup: `Backups/ParkSolarBloom-2026-11-07/`.

## Limits

These are Edit Mode/offscreen GPU checks, not full Play Mode/save-flow testing or a standalone build. No GPU performance benchmark. Effect intensity depends on view, cloud cover and exposure. Geometry/alpha-tested leaves mask the sky naturally; transparent materials depend on their depth/render setup. The flare is image-based and may also respond to exceptionally bright non-solar surfaces while the sun is in frame. No claim of matching all reference lighting/material quality or volume scattering.

Tuning: Light_Sun > ParkSunSky for solar radiance; solar sky material for angular radius; Camera_Player > ParkCinematicBloom and AmplifyBloom for lens/Bloom settings.
