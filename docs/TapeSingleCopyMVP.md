# Tape single-copy MVP

Historical MVP notes. Current three-copy behavior is described in TapeThreeCopies.md.

HouseScene runs one original A -> one accepted copy B. Three candidate work tapes remain for inspection. Commission 02/03 assets are retained but are not advanced to.

## Player flow

1. Hold LMB on ORIGINAL 01 on the desk, drag onto source deck A and release. Both bottom control groups open; B reads EMPTY.
2. Click A PLAY. Listen through the opening, interference and subsequent content. Ten seconds of audible forward playback unlock the work tapes. Rewinding before completing the audition restarts its count.
3. Click WORKBENCH. Choose a candidate, inspect both reels with LMB + scroll. Rotate with LMB + RMB. Replace broken tapes.
4. Load an intact work tape into B and try REC. Recording protection points to the holes. Eject B, use the desktop dispenser to cover both holes, then reload.
5. Click B REC. The first recording starts A and B from the beginning. STOP on B omits sounds while A continues; REC resumes B.
6. LISTEN TO COPY stops A, rewinds B and plays the actual result. SUBMIT COPY runs acceptance rules. Retry resets this recording and retains preparation.
7. One accepted copy completes the MVP. No automatic second source.

## Scene and persistence

HouseRadioDeck references OriginalTape_01, OriginalTapeHome, SourceTapeSocket, original reel pivots and auditionDuration. TapeTransportCanvas contains both groups and WORKBENCH / LISTEN TO COPY / SUBMIT COPY. Existing recorder and operation anchors are unchanged. All new objects are editor-authored.

Checkpoint v6 adds original loaded, audition progress/completion and protection discovery. Legacy commission completion is clamped to the one-copy MVP. Audio buffer/cursors retain the existing transport save. Exit stops transports. Editor Play still intentionally resets saves.

## Validation and limits

Scripted Unity checks passed: candidates hidden before audition; source pointer drag loads A; empty B visible; timed audition unlocks candidates; unsealed B blocks REC; first REC starts both; STOP B preserves A; REC resumes; listen routes to B; invalid recording rejected with reason; checkpoint round trip; one-copy completion.

These are scripted checks, not a full human playtest. Audio remains Commission_01 placeholder material. The protection explanation is a short authored text card, not final illustrated artwork. Source loading snaps to its socket without bespoke hand animation.
