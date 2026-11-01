B04 hand-held light meter / independent Prefab, Unity 2022.3.62f3 Built-in.
Prefabs/PF_LightMeter_B04.prefab. 60 x 110 x 30.185 mm; +Y up, +Z readout face.
56,120 triangles, 9 renderer meshes, 9 materials, 9 clips. Default reading unavailable.
Separate SensorHead, ISODial, ReadingNeedle, ClearCover and replaceable ReadoutContent.
Runtime/LightMeterVisual.cs is presentation only: no metering, Update, input, camera or save code.
SetIsoIndex(0..3): ISO100,200,400,800. SetHeldProgress(0..1): local motion only.
SetReadingNormalized(u,available,content): needle -55..+55 Z degrees; caller-owned Texture/RenderTexture.
ClearReading hides needle/content and clears old texture. Capture/Restore preserves sampled poses, visibility and texture.
ReadoutUnavailable clip hides both needle and content; use ClearReading to also discard retained content.
No static exposure values in model/materials. Preview example readings are runtime-only, outside Assets.
Root and operation anchors are never animated. Trigger suggestions and solid proxy disabled.
Normal maps imported as NormalMap; BaseColor sRGB; MetallicSmoothness R=metallic,A=1-roughness.
Full Blender, FBX/GLB, layered maps, parameter table, preview states and checks:
output/B04_LightMeter_v1/ at project root. Preserve Bfont-License.txt.
