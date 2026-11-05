# Three-copy tape chapter

The spring bedroom contains three originals and three independently saved working copies. Submission allows up to 0.75 seconds of interference and 2 seconds of blank tape. At least 80% of the clean source must be present, including at least 70% of each required sound. Repeated or out-of-order clean samples fail.

## Flow

- At the desk, drag the numbered original into A and play it. The first audition runs for at least 18 seconds so the player hears the first interference (14–16 s) and the return of the music. Copies 2 and 3 require playback to within 0.1 s of the source end.
- Drag a candidate onto the centre mat. Hold LMB on each actual reel and scroll to cover the tape from end to end. A broken candidate must be replaced.
- Use **Show tabs** to expose the two holes. Pull the tape head across the cutter and release the loose strip over one visible, unsealed hole of the checked cassette. Releasing on an eligible hole immediately seals it and consumes the strip. Repeat for the second hole. A rejected or cancelled patch returns to the dispenser rest for reuse.
- Rewind and play A explicitly. Hold S and F together to record B. Release F or stop B before the noise, let A continue, then hold S and F again. Releasing only S while holding F leaves blank tape. Stop A, rewind B with J and hold F to hear what was recorded.
- A successful submission saves that copy independently, shows its numbered finished cassette, clears the decks for the next copy and opens the workbench. The next original must be dragged into A manually.
- Each new work tape needs inspection and two sealed holes. Already identified broken candidates remain marked. Retrying a recording keeps the current tape preparation and does not touch earlier copy files.
- Only copy 3 completes the chapter. Task remains a single sentence; A/B status includes the current number.

## Authoring

HouseRadioDeck.originals holds three physical original tapes, home anchors, hit colliders and reel pivots. completedCopies contains three authored result displays. These props are authored in Bedroom_Spring. Existing OriginalTapeHome, source socket, radio and operation anchors are retained. New homes and completed-copy rack may be moved in the Inspector.

## Persistence and validation

Checkpoint v7 stores the current copy and existing source/audition/preparation state. v6's accepted MVP copy is interpreted as the first copy when restoring the chapter checkpoint; an already completed global story save remains subject to the existing story completion system. Editor still resets saves on each fresh Play.

Validated with scripted Unity tests: three sequential PCM recordings pass independent acceptance, successful submissions switch to the correct unloaded original, completed displays appear, the chapter finishes only after copy 3, all three saved buffers reload and pass acceptance, and chapter checkpoints round-trip. This is not a human end-to-end playtest.

Audio remains placeholder material. Commission 3 is still labeled Birthday - placeholder audio; the final birthday soundscape and final illustrated/tutorial art are not authored by this change.
