# B03 four-speed film cartridge implementation plan

**Goal:** Deliver four independent ISO variants matching approved B01/B02, a perforated deforming leader, coupled payout/rewind and receiving-spool motion, without scene/gameplay integration.
**Architecture:** Fixed root at B01 InsertAnchor. Separate carriage, shell, outlet, cartridge spool and film. Editable Blender meshes and sampled BlendShapes preserve a fixed material strip, storing unused length as a spiral inside the cassette. Coupled clips own film shape and spool rotation; no runtime input or save scripts.
**Tech stack:** Blender 3.6, FBX, Unity 2022.3.62f3 Built-in Standard.
**Spec:** User's approved B01/B02 continuation and original B03 requirements in this conversation.

- [x] Inspect current B01 geometry, scene/controller boundaries, material imports; record protected hashes.
- [x] Solve fixed-length film path through cassette outlet, over gate rails, into real C-shaped take-up slot. Author fixed 35 mm material width, physical perforations and tapered tip. Test no shaft/housing penetration in defined samples.
- [x] Author cartridge shell (approximately 25 mm diameter / 50 mm overall height), hollow hubs, light trap, ISO 100/200/400/800 band/number variants. Use existing visual language without external art.
- [x] Export editable Blender, FBX with BlendShapes, GLB preview, layered PBR maps. Import textures before materials/model binding. Normalize geometry, preserving BlendShape deltas.
- [x] Produce standalone prefabs and coupled pull/seat/advance/rewind clips, stable state restoration, local carriage insert/remove clips; no root animation, active colliders, Animator or gameplay components.
- [x] Verify actual imported geometry and arbitrary intermediate poses, reversible channels, stored strip length, take-up slot containment and final normal-map settings. Temporary B01 assembly previews only; document limitations.
- [x] Independent read-only review, fixes, preview sheets, manifest/hash-verified ZIP and Chinese handoff.

Implementation choices: a 240 mm modeled leader segment is retained through all states; the remaining roll is represented by a separate internal winding body. Mechanical loading previews do not model an entire photographic roll. TakeUpDrive is a rotary output for the existing B01 spindle, not a copied spindle or gameplay owner. User-approved existing assets remain byte-identical. Normal-map screenshot checked live: all 14 existing B01/B02 normals already report NormalMap with sRGB false.

Final: 112 Unity checks, 24 baked transport collision poses PASS. Corrected fork bore, take-up lip clearance, outlet bridges, broken color-band rings and closed-door film plane. Film sharedMesh uses compact FBX rather than a duplicate YAML morph asset. No external prefab dependency; all 567 protected files are checked at packaging.
