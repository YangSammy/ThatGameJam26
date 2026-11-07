# Photography hand-response finale

## Manual-focus amendment (same day, subsequent user request)

Removed the hand-completion autofocus write and its one-shot state. After the 0.68 s response transition, finder wheel input changes the player's focus distance again. Aperture/shutter screens retain exclusive wheel ownership. Joining, finishing the reach, rendering, and returning from a dial do not correct the player's chosen focus. Exposure acceptance and committed-photo darkroom return are unchanged. Regression evidence: `output/QA/HandManualFocus20261107/{before,after}.txt`; the updated checks first failed manual focus/no-autocorrection, then passed after the change. Earlier autofocus descriptions/evidence below describe the initial version, superseded by this amendment.

Implemented the user-approved ungraded photography flow. Aperture, shutter, focus and composition still affect rendered images/metadata, but are not success gates. Film loading, three story checkpoints, response timing, hand click, available frames and pending-save safety remain.

After the saved hand response, finder/aperture/shutter are the only allowed camera modes. Back returns from a dial to finder; lowering, metering, focus scrolling and stage shortcuts cannot exit the response. Dial handoffs preserve the existing contact phase. System pause/focus handling remains.

ParkHandResponseView offsets only the rendering viewpoint by 0.10 m over the existing reach, with one 3 mm step and <=0.2 degree roll; it restores both camera and compensated arm root after rendering. Girl animation/AI and player bone solving continue using the original reference transforms. Autofocus and preview use the effective optical depth.

Only durable final joined exposure acknowledgement queues the existing brush scene transition. Contact stays visible during cover. An acknowledged film is reloaded before leaving. Failed saves stay in locked finder and reuse the existing retry pipeline. Explicit darkroom intent survives asynchronous House/Summer binding and is cleared only after arriving at the authored darkroom view. No models, bones, girl animation, scene assets or save schema were replaced.

## Evidence

- `output/QA/HandFinale20261107/before.txt`: observed old navigation/quality failures before implementation.
- `checks-final.txt`: 54 aperture/shutter combinations, locked navigation and stage shortcut, preserved timer/focus, ten render/restore cycles, matching optical depth; existing camera-edge and exposure recovery checks passed.
- `Play/Reports/recording.json`: final end-to-end PASS, 29.78 s, no recorded errors.
- `Play/Reports/committed.json` and `darkroom.json`: three accepted records, including entrance f/2.8 at 1/4 s (~-6 EV error), bridge f/16 at 1/1000 s (~+5.47 EV error), and joined final f/16 at 1/1000 s. Darkroom ready and DirectDarkroomActive true.
- `Play/Reports/save-failure.json`: injected final save failure retained the locked camera and two existing frames; retry committed exactly one final frame before transition.
- `Play/Reports/contact.json`: approach settled at 0.100000225 m. Manual renders and dial navigation left the existing hand/girl transforms unchanged. Pause pose remained stable.
- `MotionCaptureRun/Reports/recording.json`: independent fixed-step capture pass. `camera-approach.gif`/`.mp4` use actual rendered frames at game-time durations, not synthesized motion. `Play/darkroom.png` shows the verified destination.
- ParkGirlPresentation SHA256 unchanged: `9B2A27390DE370FFC575AB037BB17D37BC2C3C3D0C5F5B6C9F78A4A9453DBF8B`.
- Final Console errors: zero; scoped diff whitespace checks passed. Editor stopped with clean scene, capture timestep reset to zero and EditorPlaySession restored with byte-tree equality.

Validation drove existing handlers/checkpoint progression and shortened walking; native mouse/keyboard gestures and a standalone build were not exercised. Unrelated dirty workspace changes were preserved. SharedHouse's pre-existing non-UTF8 bytes were preserved by an ASCII-only byte replacement.
