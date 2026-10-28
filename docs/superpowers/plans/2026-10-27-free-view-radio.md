# Free-view radio interaction

The user approved direct tape/radio interaction from HouseScene exploration, retaining E safe inspection and leaving other chapters unchanged.

- Remove chapter-zero fixed-camera entry. Preserve the authored radio and operation anchor transforms.
- Empty-handed interaction uses the exploration center ray and existing visibility/distance checks.
- Holding a cassette frees the pointer and temporarily stops walking/look at the player's existing pose, enabling spindle scanning and planar dragging. Loading or returning it restores exploration. Esc returns the held cassette.
- Keep a single input owner in WBMain. Prevent held-tape clicks entering another chapter and prevent distant right-clicks resetting the radio.
- Keep radio loading/playback active during exploration; pause when entering another chapter and retain existing inspection behavior.
- Route chapter completion by its actual model rather than the previously selected chapter index.
- Update Play regression coverage for direct pickup, unchanged camera, pointer ownership, drag-loading, inspection, and unchanged other-chapter transitions. Keep the four-frame photography roundtrip coverage.

No scene layout or anchor edits are required.

## Verification

HouseGameplayChecks passed 161 assertions in Play Mode after updating the expectations for free-view radio and four-frame film. Additional production-handler checks opened and closed safe inspection for the radio and a held cassette. Unity Console had no error entries at the final check. These are automated interaction checks, not a physical mouse/keyboard usability playthrough. Editor returned to HouseScene outside Play Mode.
