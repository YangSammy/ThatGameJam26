# Seasonal window ambience

User approved: only outdoor seasonal SFX grows louder near the window. Keep BGM and SFX-001 global. Reuse SFX-002 season clips and the existing day-five window loop. No save/layout changes.

Implementation: scene-authored BedroomWindowAmbience anchor per bedroom, persistent GameAmbience voices use 3D linear rolloff (1.25–7 m, no Doppler). Park remains diffuse 2D; day five suppresses the regular spring voice to avoid doubling. Missing/disabled anchors fade silent. Scene load/unload refreshes cached bindings; no per-frame scene searches.

- [x] Add runtime binding and compile.
- [x] Back up and author four window anchors without moving existing objects.
- [x] Check references, season selection, distance settings, lifecycle and fifth-day suppression in Unity. Preserve original editor/save state.
- [x] Provide Inspector location and concise manual listening checklist. No native mouse test or build requested.
