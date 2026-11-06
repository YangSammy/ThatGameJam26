# Writing movement and farewell fix — 2026-11-06

Requested: increase memory-room movement speed to 1.5x; investigate and fix farewell stalling after collection.

Reproduction before fix:
- Begin a held paper drag, enter the arrival region with all items, then continue PointerMove without another PointerDown: WritingProgress delta remained 0. Arrival called CancelInput and cleared pressed/havePointer while world writingHeld stayed true.
- At arrival, hold E and deliver valid rightward mouse movement every other render frame for 3 seconds: phase remained arrival and farewell progress stayed 0. The per-frame fresh-ink rule reset the 1.2s hold on every empty input frame.

Fix:
- WalkSpeed 22 -> 33 model units/sec (exactly 1.5x).
- Arrival still freezes countdown and clears pickup intent/hold progress, but preserves the held paper gesture.
- Farewell progress only increases on fresh ink while E is held. Input gaps <=0.25s pause progress; longer stops or E release reset it. E alone and stationary pen cannot complete. Pickup freshness remains strict and unchanged.

Validation:
- Added FarewellContract; confirmed failing before implementation.
- FarewellContract passes at 30/60/120 Hz: arrival preserves drag, sparse ink can complete, idle cannot advance, E release resets, two-second farewell completes chapter.
- Existing NarrativeRegressionChecks: 12 passed, 0 failed.
- InputContract and AutumnRestorationChecks.RunModel pass.
- Recommended route now 25.366s at 60 Hz, with 34.634s remaining; the earlier 35-45s target is superseded by the requested speed increase.
- Unity Console errors 0; git diff --check passed.
- No active user writing station was available for reproduction, so validation used production rule objects and scripted inputs, not a native-input playthrough.
