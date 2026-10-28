# Three-copy tape chapter

The single-copy MVP is now expanded to three originals and three separately saved copies. Existing Commission_01/02/03 definitions are used. All three use relaxed development acceptance: up to 2 seconds of interference and 2 seconds of blank audio, 80% clean coverage and 70% of each required opening/ending segment. Short interference segments may now pass without removal. Preparation and ordering checks remain unchanged.

## Flow

Direct interaction: no recorder/station entry is required. Hold LMB on the current original to drag it into A. Click an available work tape to place it on the desk inspection anchor. Click the recorder to reopen its controls, or click tapes/tools directly from those controls. The exploration camera pose remains unchanged. During manipulation movement/look are suspended. Esc first cancels an active drag/gesture; a further Esc leaves the workbench. Exiting stops both transports and saves progress. Existing audition and sequential-original rules remain.

- Drag the current numbered original into A, audition it, then prepare a work tape for B.
- Copy 1 retains the initial ten-second audition. Copies 2 and 3 ask for almost the complete original (duration minus 0.1s), including material after their interference.
- RECORD starts the first recording from the beginning; STOP pauses B only; RESUME REC continues writing. PLAY COPY does not write. LISTEN TO COPY stops A and rewinds B.
- A successful submission saves that copy independently, shows its numbered finished cassette, clears the decks for the next copy and opens the workbench. The next original must be dragged into A manually.
- Each new work tape needs inspection and two sealed holes. Already identified broken candidates remain marked. Retrying a recording keeps the current tape preparation and does not touch earlier copy files.
- Only copy 3 completes the chapter. Task remains a single sentence; A/B status includes the current number.

## Authoring

HouseRadioDeck.originals holds three physical original tapes, home anchors, hit colliders and reel pivots. completedCopies contains three authored result displays. All exist in HouseScene, without runtime spawning. Existing OriginalTapeHome, source socket, radio and operation anchors are retained. New homes and completed-copy rack may be moved in the Inspector.

## Persistence and validation

Checkpoint v7 stores the current copy and existing source/audition/preparation state. v6's accepted MVP copy is interpreted as the first copy when restoring the chapter checkpoint; an already completed global story save remains subject to the existing story completion system. Editor still resets saves on each fresh Play.

Validated with scripted Unity tests: three sequential PCM recordings pass independent acceptance, successful submissions switch to the correct unloaded original, completed displays appear, the chapter finishes only after copy 3, all three saved buffers reload and pass acceptance, and chapter checkpoints round-trip. This is not a human end-to-end playtest.

Audio remains placeholder material. Commission 3 is still labeled Birthday - placeholder audio; the final birthday soundscape and final illustrated/tutorial art are not authored by this change.
