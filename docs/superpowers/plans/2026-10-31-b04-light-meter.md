# B04 hand-held light meter implementation plan

**Goal:** Independent original hand-held meter, matching B01–B03, with separate sensor head, four-step ISO dial, program-driven needle/content surface and reversible presentation motions.
**Architecture:** Stable root; motion pivot carries the physical shell. Fixed anchors outside animation bindings. A small presentation-only component accepts ISO, normalized reading, availability, optional content texture and held progress; no photometry, input, saves, Update or scene dependencies. Clips provide equivalent mechanical channels.
**Tech stack:** Blender 3.6 editable meshes; FBX/GLB; Unity 2022.3.62f3 Built-in Standard.
**Spec:** User's B04 request and original module specifications in this conversation; approved B01–B03 style and standalone delivery boundary.

- [x] Read current Park/House meter code and existing authored assets; snapshot existing art, scenes, scripts, project settings and package hashes.
- [x] Author approximately 110×60×30 mm complete shell, back cover, grip surfaces, incident diffuser, independent ISO dial, open display bezel, separate clear cover, pivoted needle and blank replaceable content surface with stable UVs.
- [x] Create visual API and motion clips: raise/lower, ISO detents, normalized readout response and unavailable state. No static exposure numbers, input changes or gameplay calculations. Preserve arbitrary intermediate pose on capture/restore.
- [x] Import real FBX, correct normals before material binding, independent prefab, editable PBR layers and glass Alpha source. Validate program-driven textures/needle, clipping, independent channel updates, pause/reverse/restoration and root invariance.
- [x] Render actual Unity default, all ISO values, raised/lowered/midpoint, multiple runtime-only reading examples, unavailable and complete rear/bottom views. Inspect near-camera appearance.
- [x] Fresh read-only review; fix concrete issues; manifest and protected hashes; complete ZIP and Chinese documentation.

Rulings: use a needle plus blank replaceable content panel, rather than baking one exposure recommendation. ISO numerals and anonymous scale ticks are mechanical markings. Head is structurally separate but has no invented swivelling interaction. Raise/lower are model-local suggested motions and do not move any camera or existing grip/operation target. Hand fitting and actual metering remain future integration work.

