# B01 camera body sample production plan

**Goal:** Deliver one original, real-size, editable mechanical 35 mm camera body for user inspection before expanding the asset list.

**Spec:** User's B01 and common asset requirements in this chat, 2026-10-31.

**Architecture:** Add only `Assets/Art/Props/CameraBodyB01` and `output/B01_CameraBody_v1`. Blender source with separate transform pivots, FBX export, Standard materials, independent Unity prefab and manually sampled animation clips. No gameplay component, input, scene, anchor, ID or save changes.

**Tech stack:** Blender 3.6.23, editable procedural texture sources, Unity 2022.3.62f3 Built-in Standard.

## Production decisions
- B01 first: its geometry, internal cavity and moving controls exercise the user's most important requirements. Tape assets already have independent production directories; making a new camera sample avoids overwriting them.
- Approximate closed dimensions 14 x 9 x 5 cm, lens excluded. Lens mount and internal mirror box present; B02 lens and B03 film are separate future deliverables.
- Warm grey satin shoulders, charcoal enamel, subtle molded grip, localized contact wear; original unbranded design. Only mechanical numbers/index marks.
- Stable root; rear left hinge; liftable rewind axis and concentric rotating knob; right shutter-speed dial, advance lever, short-throw shutter, bottom release, take-up spindle. Default closed, unloaded, controls released.
- Clips contain only specific visual transform channels; no root motion, auto-playing Animator, gameplay events or saved-state ownership. Pause by retaining sample phase; cancel/restore by reapplying the owner's stable pose snapshot. Multiple completed channels are retained explicitly.

## Work and checks
- [x] Inspect configuration, camera creation, inspection bindings, restoration code and existing scene model structure; record protected hashes and Console baseline.
- [x] Build complete editable mesh, real cavities, pivots, UVs, layered PBR sources and closed/open/action previews in Blender.
- [x] Export FBX, import in Unity, build independent Standard materials/prefab and animation clips using Editor API without opening/saving production scenes.
- [x] Verify scale, orientation, normals, positive transforms, materials, every clip endpoint/midpoint, root invariance, pause/reverse/restore, collider separation and retained committed presentation channels.
- [x] Render actual Unity imported prefab states; inspect images. Save measurements, triangle/material/texture counts, source/license record, motion table and limitations.

## Execution record
- First import exposed exporter handedness and fresh-empty anchor evaluation issues; corrected in editable source before final authoring. Pivot axes normalized in derived Unity meshes, no negative scale.
- Independent read-only review identified Unity single-axis position curves zeroing missing X/Z channels. New lateral-invariance assertion reproduced failure on old clips; fixed with constant X/Z curves. Final isolated checks: 103 PASS, 11 clips.
- Review minor mechanics addressed: real open C-section take-up slot; rewind fork parented to rotary spindle; matching Blender/Unity dial step directions; removed unused empty hit grouping.
- Final sample: 141.5 x 93.15 x 51.14 mm, 83,452 Unity triangles, 7 materials, 9 renderers. No gameplay or save-retention integration claim. User confirmation of the sample precedes later modules.

## Review focus
- Metric FBX conversion and local motion axes can differ from Blender: measure imported vertices/pivot motion before authoring Unity curves.
- Film compartment must remain physically empty and accessible when the rear door opens, with real 35 mm gate/rails.
- Animation must never change gameplay roots, save state or existing runtime colliders.
- Mouse hit suggestions are disabled by default and must not block walking or existing interaction rays.
- Confirmation is on the delivered first sample; no scope expansion or replacement of scene art during this task.
