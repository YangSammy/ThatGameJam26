B01 Camera Body — standalone close-inspection sample, 2026-10-31

Use Prefabs/PF_CameraBody_B01.prefab. Do not replace scene objects automatically.
Unity 2022.3.62f3, Built-in Standard. No gameplay/input/save scripts or autoplay Animator.
141.50 mm wide x 93.15 mm high x 51.14 mm deep overall, lens excluded.
83,452 imported triangles, 9 referenced meshes/renderers, 7 materials, 11 clips.
The original FBX plus normalized *_Mesh.asset files are both supplied.
The prefab references normalized meshes with +Y up / +Z lensward / +X photographer right.

Materials: BaseColor sRGB; Normal imported as Normal Map; MetallicSmoothness linear
R = metallic and A = 1 - roughness; AO grayscale G. Editable separate maps retained.
Resolution: enamel/grip 1024, satin metal/cavity 512, brass/optics/markings 256.

Animation: all paths begin Visual/; no root or anchor channels.
Pause by retaining phase; cancel to a selected phase or restore an owned local-pose snapshot.
Position clips preserve X/Z explicitly. Do not reset all channels to cancel one gesture.
Lift rewind shaft before opening the back. Gameplay owns loading/fogging/committed progress.
Disabled per-part Hit_* triggers and separate PhysicsProxy_DISABLED are suggestions only.
No HouseScene or ParkScene binding, real mouse/hand-fit, save roundtrip or player-build claim.

Source, full Chinese handoff, detailed movement table and actual Unity state renders:
output/B01_CameraBody_v1/ (project root).
Validation: 103 isolated Unity EditMode assertions, no production scene entry or save writes.
Model/textures authored for this sample. Mechanical glyphs use Blender Bfont;
preserve the accompanying Bfont-License.txt on redistribution.
