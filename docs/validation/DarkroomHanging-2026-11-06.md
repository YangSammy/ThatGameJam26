# Darkroom vertical hanging

- User requires both negative and print to hang downward, not lie flat; this is not a landscape/portrait aspect-ratio request.
- Current authored scene reproduced vertical materials when fully hung. The old pose code nevertheless copied the holder's complete rotation; rotating the holder 90 degrees produced a horizontal material up-axis. The exact reported flat pose was not reproduced in the unmodified current layout.
- Changed hanging orientation to retain horizontal heading while keeping local Y aligned with world up. Grip positions still come from PaperGrip/PaperHangAnchor; existing Hanging deformation, flat tray poses, and scene layout are preserved.
- Executed actual PoseDarkroomMaterial on a cloned scene binding for both materials with holder rotations (0,0,0), (90,0,0), (0,0,90), (35,70,22). All eight cases had vertical transformed up-axis (dot > .999) and attachment error < 1 mm.
- Before the change, the tilted-holder orientation check reproduced a horizontal up-axis. After the change, all eight cases passed. Unity Console contained zero errors.
- Current mesh sampling confirms Hanging=100 makes both materials extend downward from their upper origin. The paper's landscape proportions are retained.
- Ordinary Windows rebuild result: see output/QA/DarkroomHang/build.txt. Actual executable interaction has not been retested for this change.
