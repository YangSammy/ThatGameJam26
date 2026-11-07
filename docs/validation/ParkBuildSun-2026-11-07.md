# Park Build sunlight investigation

Report: user sees little sunlight and no character rim in standalone Build.

## Evidence
- Running process: D:/GameJams/ThatGamejam2026/Build/Remember seasons.exe. Data/assembly build time 2026-11-07 02:01:47 local, before final natural color tuning. Player.log initializes Unity 2022.3.62f3 with D3D11 / RTX 4060; inspected tail has no shader/runtime exception.
- Actual running window inspected via native screenshot. Entrance path/character are deeply shaded, while right grass slope and tree crowns receive visible sunlight. The screenshot does not show a globally missing directional light.
- Read built binary `level2` using isolated `uv run --with UnityPy` (not a project dependency): Light_Sun active/enabled, directional, intensity 1.28, warm color (1,.91,.76), realtime (m_Lightmapping 4), soft shadows strength .9. Camera_Player active, HDR enabled, skybox clear. Park Character Backlight active/enabled, serialized intensity 3, spot range3.8, ForcePixel. Both Entrance_Canopy_Shadow and Bridge_Canopy_Shadow active with ShadowsOnly renderers. Built assets include ParkSolarSky and ParkClearDayBeautify; assembly contains ParkSunSky, ParkCharacterRimLight and ParkCinematicBloom. Arbitrary MonoBehaviour typetrees were stripped, so built runtime parameters were not inferred from incomplete deserialization.
- Current authored entrance reproduction through live Editor: camera forward (.57,0,.82), direction toward sun (-.70,.52,.49); solar optical viewport gate=0. Sun is offscreen at entrance.
- Character at (-12,.09,-9): sampled sunlight visibility0; rim intensity3 (configured shade3/sun60).
- Isolated reversible experiment: removing only rim's authored ShadowsOnly mesh list changes sunlight visibility0 -> 1. Restored list immediately and called UpdatePose. This demonstrates those authored canopies fully block the character's direct-light test at the entrance.

## Conclusion and limits
The inspected entrance view is explained by an intentionally large authored shadow region, the sun-aware weak-rim rule, and offscreen solar optical gating. The built sun/light resources are present. Old build timing explains missing latest color refinements, but is not evidence of a missing sun. No production settings were changed. Recommended artistic correction is to reshape/shrink the entrance shadow-only canopy or create a lit opening in the path, then supply suitable ambient fill for shaded characters; preserve the requested sun-strong/shade-weak rule. New build and full route validation would be needed after such a change.

This investigation did not read live managed values inside the standalone process, produce a new build, change user saves, or modify lighting behavior. Built serialization, actual player screenshot, current-source logic and the controlled Editor occlusion experiment are distinguished above.
