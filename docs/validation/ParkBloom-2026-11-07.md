# Park Bloom — 2026-11-07

Implemented the user-approved subtle park Bloom using imported Amplify Bloom 1.3.2, Unity 2022.3.62f3 Built-in/Gamma.

## Saved configuration

`Assets/Scenes/ParkScene.unity`, `Camera_Player`: added AmplifyBloom and AmplifyBloomEffect after existing ParkPhotoFocus. Explicit main/composition shader references serialized for dependency retention. HDR enabled already.

- Threshold 0.72, intensity 0.25, High precision.
- Realistic, Half threshold resolution, 4 mips, weights 0.4/0.3/0.2/0.1.
- Lens dirt, starburst, flare, glare, bokeh and temporal filtering disabled.
- Existing scene lighting, materials, scripts, photography state and saves unchanged.

Ruling: original proposed HDR threshold 1.2 produced effectively no visible contribution in this scene (mean absolute difference under 0.002/255). Tuned threshold to actual scene brightness rather than increasing light intensities. Water has limited intrinsic highlights; this change does not add reflections.

## Verification

- Captured 1280x720 before/after at entrance, tree/bridge approach and waterside; inspected images. Output: `output/ParkBloom-2026-11-07/`.
- Actual scene camera renders into ARGB32 correctly with Bloom. HDR-vs-ARGB32 offscreen comparison had mean absolute channel difference approximately 2.19/255 at entrance; not a claim of pixel-identical screen/capture output.
- ParkPhotoFocus does not execute automatically in Edit mode. Initial camera-only focus sweep was therefore invalid as a focus test. Replaced it with direct invocation of the actual focus render method followed by Amplify Bloom using HDR intermediate textures.
- Corrected combined render sweep: overexposed mean 130.58, neutral 83.31, underexposed 40.11 (0–255 channel scale); defocus visibly blurred; full raise occlusion exactly black (max 0). Test state/materials restored afterward.
- Shader support/references checked; Console warning/error query empty after rendering.
- Scene saved through Unity; diff adds plugin components and its generated gradient texture only. Existing MainMenu edit retained.

Backup: `Backups/ParkBloom-2026-11-07/ParkScene.before.unity`.

Limitations: no Play Mode walkthrough, full film/save workflow, standalone build or GPU profiler benchmark. Visual improvement is intentionally restrained; frame-rate improvement is not claimed.
