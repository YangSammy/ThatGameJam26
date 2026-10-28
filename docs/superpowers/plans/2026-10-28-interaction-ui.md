# Compact interaction UI

User request: apply the inspection UI's compact English presentation to all live direct-operation prompts. Preserve necessary actions and tape-break feedback.

Shared InteractionUI renders readable text with a subtle shadow and lightweight action buttons. Remove large panel backgrounds from house, tape, photo loading/development/gallery, and park photography. Keep viewfinder framing and photographic images. Safe inspection shares the button renderer.

Use short English action text, current-step prompts for writing/painting/scarf, scan percentages and explicit broken-tape feedback, film counts, development timers, and recoverable error notices. Keep actual gameplay and scene transforms intact. Photo UI consumes pointer input only over visible action buttons or modal photo gallery, instead of reserving a full-width footer.

Pickup remains a direct hold-and-drag action. The earlier removal applied to entering the tape station's fixed camera, not picking up tapes. E continues to open a separate safe inspection display.

Verification: Unity compiled the shared InteractionUI successfully. HouseGameplayChecks passed 165 Play assertions, including English help for the four fixed-view chapters, tape pickup/break scanning/loading/playback, inspection, other chapter transitions and the four-frame photography/development roundtrip. Final Console query returned no errors. This is automated handler/scene validation, not a manual visual review at every resolution. Play was stopped after verification.
