# Winter paper art integration

Goal: apply approved minimalist scenery and scarf-free sprite to both paper platform stages, preserving chapter rules and observation/ward assets.

Architecture: a presentation-only partial on PaintingWorld3D builds layered scenery and flat mechanism art in route coordinates. Existing state, anchors, colliders and pose sampler remain authoritative. Hide old rendering only while paper art is active, restore it on deactivation/disposal. Preserve real pendant recess and interaction geometry.

- [x] Import clean background plates and 24-frame transparent character atlas with explicit texture settings.
- [x] Add isolated rendering and deterministic frame selection driven by movement, velocity and existing state. Align feet and avoid baked jump travel.
- [x] Replace stage surfaces and moving mechanism presentation; preserve dynamic swing seats, horizontal wheel carriers, bridge growth, plate walkway and door.
- [x] Compile in Unity; render both phases and acquired/unacquired states; validate state invariance and restore. Both platform model traversal checks passed; no live input walkthrough performed.

Validation uses an isolated preview scene, not saves or chapter completion in the open ParkScene. Existing unrelated modifications are retained. Generated character jump frames are a draft; use foot-aligned cells and model-driven vertical motion.
