# Park photo points 2/3: golden viewfinder

User approved the discussed effect and explicitly requested implementation without tests.

- Extend the existing ParkCinematicBloom owner, preserving its render order before Beautify. Restrict the new optical wash to an arrived Bridge/Meadow photo stop, raised viewfinder, nearby player and sun in view. Smooth entry/exit; suppress during camera occlusion, instrument detail views and photographic capture/background sampling.
- Add a serialized material/shader dependency so player builds retain the shader. Probe original pre-bloom HDR solar pixels into a 1x1 visibility history on the GPU, then add spatially localized golden veiling light after Amplify Bloom. No CPU readback or gameplay exposure changes.
- Reduce normal-walking Bloom and remove generic lens ghosts for this profile. Keep large golden light in the viewfinder rather than on the character surface.
- Reduce the physical character rim to sunlight 4 / shade .4; align it to sun direction, enable self-shadowing and exclude first-person hands/instruments. Disable overlapping bridge/meadow strong spotlights. Gently increase neutral environment fill.
- Keep existing sun direction, backdrop openings, natural grading, materials, route, camera focus/exposure mechanics and meter presentation.
- Preserve current working changes and back up the scene before targeted YAML updates. Do not run compilation checks, automated tests, Play Mode, preview renders or builds. Final appearance remains unverified by user request.
