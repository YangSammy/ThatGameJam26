# Player hand refinement implementation plan

Goal: improve close-up knuckles, thumb web and wrist while preserving the existing skeleton and handhold behavior.

Approved scope: the user's Blender refinement request in this chat. Keep bone names, hierarchy, bind poses, animation controller, handhold code and girl assets unchanged. Update both full arms and the Resources left-only mesh.

Approach: work on an FBX-imported copy in background Blender; author local topology and surface/weight smoothing, retaining UVs. Transfer only mesh data into Unity mesh assets against the original 52 binding indices. This avoids FBX skeleton/animation reimport changes. Keep the source FBX unchanged. Reuse the active checkout and connected Editor; unrelated work is already present.

- [x] Capture source data, reference images, prefab and left-arm backup; run existing gesture checks as baseline.
- [x] Refine a Blender copy, verify coordinate round-trip, bone identities, weights and surface continuity; save editable .blend and interchange mesh data.
- [x] Create full-arm mesh asset, update prefab mesh references and update left-arm mesh in place, retaining its GUID.
- [x] Run existing full-arm and front/meadow gesture checks; inspect close-up, reach and release images against baseline.
- [x] Verify original bind poses, runtime code and animation resources unchanged; document results and limits.

Acceptance: visible improvement without changing palm position, finger lengths or skeletal controls; normalized finite weights, valid UVs/normals and no missing references; left-only uses refined geometry; existing gesture checks pass. Images are isolated real-model Unity renders, not a full gameplay playthrough.

Execution: original .blend could not be read by available Blender 4.2.9, so authoring uses the same source FBX as Unity. FBX mesh coordinate match against original Unity vertices is verified to 0.000000134 m. Source FBX and original skeleton remain untouched. The original 512px texture is preserved; remaining painted faceting is not treated as a geometry failure.
