# Seasonal bedroom transitions

Approved direction: keep HouseScene as the existing system host, but never reveal its legacy room during startup, park return, season changes or winter album entry. Preserve scene layout, authored anchors, gameplay and save format. Rapid iteration: code/compilation checks and a manual checklist; no native mouse testing.

Implementation:
- [x] Centralize seasonal loading behind a full-screen fade. Block input until the target scene is loaded, bindings restored and camera placed; retain the cover with retry on failure.
- [x] Remove legacy-room activation and camera/spawn restoration from bedroom teardown. Keep winter's authored album world and necessary lighting, without revealing the old room. Route development shortcuts through the same handoff.
- [x] Inspect startup/park return paths, compile where available, document manual season/return/retry checks and limitations.

Files: WBMain.SpringBedroom.cs for scene binding; new WBMain.BedroomTransition.cs and BedroomTransitionOverlay.cs for transition lifecycle/UI; WBMain.SharedHouse/Input/DebugChapters for input/entry integration; SpringTapeRoom, SummerPhotoRoom, NarrativeBedroom, DepartureBedroom for cleanup/presentation.

No serialized scene or save data migrations. Existing system-host load paths remain until a separately authorized full system-scene extraction.
