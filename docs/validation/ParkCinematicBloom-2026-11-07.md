# Strong park Bloom and backlight — 2026-11-07

User requested more exaggerated character backlight, raised-camera optics and stable small highlights.

## Implemented

- Camera_Player: ParkCinematicBloom replaces AmplifyBloomEffect as the single image-effect adapter. The imported AmplifyBloom renderer remains responsible for all Bloom/flare processing; its source is unchanged. Adapter follows existing ParkPhotoFocus in component order.
- Bloom: threshold 0.8, High precision, Realistic, Half resolution, 5 mips; weights .25/.25/.23/.17/.1. Overall intensity .75 walking, 1.4 viewfinder. Separate lens threshold .12.
- Viewfinder optics: Lens_Old_2 dirt strength 2; flare .75 with 3 ghosts and halo; horizontal glare .55. Raised-camera optical state blends in/out over approximately .2 seconds in Play Mode. Bokeh and starburst remain off.
- Temporal filtering .45 for preview, cleared on large pose/FOV discontinuities, occlusion, disable/re-enable and capture. Existing suppressFocusAid scope identifies every actual shutter/background sample; these retain walking Bloom but exclude lens effects and temporal history.
- Park Character Backlight: warm spotlight, intensity 60, range 3.8, angle 40, no shadows, ForcePixel. Position is relative to subject and viewer, behind and to the side, targeting upper torso. This is stylized cinematic lighting, not physically fixed sunlight. It can illuminate nearby surfaces in its narrow cone; actor layers/materials are unchanged.

## Verification

- New runtime and Editor scripts compiled in Unity.
- `MemoryWhitebox.Editor.ParkCinematicBloomChecks.Run()` => 11/11 checks passed, capture difference 0/255.
- GPU tests cover walking/viewfinder/capture flags, capture independence from preview history, black occlusion, temporal smoothing of disappearing highlights, walking restoration, camera-cut reset, disable and re-enable, and the shared photographic render entry.
- Native 1280x720 scene renders inspected for entrance and closer character composition, walking and viewfinder variants. Screenshots in `output/ParkCinematicBloom-2026-11-07/`. These are Edit Mode camera previews, not full gameplay screenshots.
- Final Console query contained no project errors; one MCP websocket reconnect warning accompanied domain reload.
- Scene saved through Unity, no duplicate plain AmplifyBloomEffect. Original camera transform/FOV restored.
- Backup before this revision: `Backups/ParkCinematicBloom-2026-11-07/ParkScene.before.unity`.

## Limits

No full Play Mode walk/photo/save regression, standalone build, motion-sequence visual inspection or GPU profiling. Temporal smoothing is tested on synthetic HDR inputs; real foliage movement still needs gameplay assessment. More effects and one realtime light add GPU cost. Concurrent Ward and MainMenu work was left untouched.

Tuning: Camera_Player > Park Cinematic Bloom for walking/viewfinder strength, lens strength and temporal weight; Amplify Bloom for thresholds/mips; Park Character Backlight > Light for rim strength.

Independent review found the generated-reference camera bypassing the capture guard. Fixed ParkPhotoPrototype.Bridge.cs to use RenderPhotographicFrame(); only the guarded renderer now directly calls view.Render(). Reviewer confirmed resolution, no remaining blockers in focused review.

