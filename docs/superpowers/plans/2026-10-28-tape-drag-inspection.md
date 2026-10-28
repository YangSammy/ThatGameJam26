# Tape dragging and inspection

Remove the persistent hand-held cassette state. A cassette can only be dragged while LMB is held; it stays on a camera-parallel plane through its initial world position, preserving its orientation. Release on the slot loads it; release elsewhere, RMB, Esc, or focus loss returns it. The player view remains at the approach pose during dragging. Existing radio and scene anchors remain unchanged.

E inspection owns reel checking through the existing single-gesture input path. Hold LMB on a displayed reel and scroll (or drag the dial) to advance the corresponding real tape's scan record. Broken reels stop; a complete intact scan enables the existing playback completion condition. Displayed reels and damage marks reflect this record. Closing inspection preserves the tape's scene placement and retains scan evidence; Reset View resets framing, not evidence. Other objects' inspection parts remain presentation-only.

Remove obsolete hand-position settings and the hand pose that followed the held cassette. Keep loading animation, playback, other chapters and English UI.

Validation targets: no eye-position jump; no persistent state after off-slot release; no world-space reel scanning; inspection wheel updates the correct candidate; reset and exit retain findings; broken insertion rejects and returns; intact drag-load plays; RMB on the radio ejects the loaded cassette directly to its home; other chapters and four-frame film roundtrip remain functional. The slot proxy obscures the loaded cassette from the current approach, so right-click ejection is the accessible removal route.

Verification: HouseGameplayChecks passed 171 assertions in Unity Play Mode, including actual inspection reel rays and scroll events, view-reset evidence retention, direct drag cancellation, broken-tape rejection, intact playback, and four-frame film development. Final Console query returned no errors. Stopped Play after testing. This is automated production-handler validation, not manual mouse usability testing. The temporary editor reconnection script was removed after the session recovered.
