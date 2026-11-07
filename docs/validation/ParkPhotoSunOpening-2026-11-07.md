# Photo points 2 and 3: backlight and background opening

User requested strong backlight at the bridge and meadow photo points, then explicitly allowed removal of background canopies.

## Saved changes

- Sun rotation (12, 256, 0), directional intensity 1.28. Sky direction follows the sun; HDR disc radiance is (120, 104, 82).
- Disabled the renderers of ForestLayer_7, ForestLayer_0, Near_Sector_3, Far_Sector_2 and MountainLayer_0. Geometry probes identified these overlapping forest/ridge backdrop cards along the two sun sightlines. Original objects and assets remain available for restoration.
- DistantForest_Backdrop remains enabled, using a cloned ForestArc_R90_PhotoSunOpening mesh. Only 17 of 162 vertices changed: upper canopy vertices lowered around azimuth 76 degrees, with a smooth transition to unchanged sectors. Original ForestArc_R90 asset and all 38 terrain tree instances remain unchanged.
- Character sunlight rim is 60, shaded rim remains 3, warm-white color (1, .9, .75). The earlier trial at 85 produced excessive flare on the bridge and was reduced after opening the actual sun sightline.
- Lens flare 1 and glare .3 preserve the raised-camera effect while reducing obscured character detail. Walking/viewfinder Bloom intensities, Beautify grading and gameplay camera behavior unchanged.

## Evidence and limitations

- Saved and reloaded ParkScene before rendering four final native 1280x720 HDR camera views. Visually inspected final-bridge.png and final-meadow-viewfinder.png: sun visible through a tree gap at point 2 and in open sky at point 3; stronger backlit hair/shoulder edges and lens bloom with readable clothing.
- Images: output/ParkPhotoSunOpening-2026-11-07/final-{bridge,exit-path,meadow,meadow-viewfinder}.png. Bridge and meadow-viewfinder captures enable the existing aiming effect; edit-mode renders do not reproduce the gameplay raising animation.
- Serialization comparison against Backups/ParkPhotoSunOpening-2026-11-07/ParkScene.live-before.unity confirms exactly nine intended component blocks changed, no objects added/removed, and no incidental renderer-save changes retained. New mesh and Unity-generated meta verified present.
- Solar, rim and cinematic checks previously passed 33/33 after the sun-angle adjustment. A post-opening rerun and extra raw-HDR pixel probe could not be confirmed because Unity MCP disconnected; Editor.log records WebSocket connection closure and Unity remains responsive. Those additional attempts are not reported as passing.
- Scene/material/mesh changes are saved. No executable rebuilt; target-player validation remains outstanding.

Backups: Backups/ParkPhotoBacklight-2026-11-07 (before sun adjustment) and Backups/ParkPhotoSunOpening-2026-11-07 (before backdrop opening).
