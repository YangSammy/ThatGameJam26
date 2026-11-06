# Left-hand meadow gesture

Approved scope: existing left arm and girl's right hand; open reach, contact grip, release and withdrawal. No gameplay, inputs, save fields, scene anchors or model replacement.

Implementation sequence:
1. Extend existing editor arm checks to reproduce premature closed grip.
2. Add a presentation-only phased gesture to FirstPersonHands; keep all authoritative state on ParkPhotoPrototype. Drive existing Generic rig, preserve desk behavior, use scaled time and freeze on interruption. Reuse left-only mesh during withdrawal.
3. Calibrate palm basis from existing bones, moderate grip and synchronize girl's closing fingers with contact. Shutter samples current animation without skipping time.
4. Editor compilation, fixture checks with real models, baked-pose previews, cancel/pause/reentry checks. No real mouse tests. Record visual limitations and manual checks.

Alternative rejected: importing paired Humanoid clips would require new rig retargeting and wouldn't solve variable contact positions. Use a procedural short animation and existing finger clips.

Rollback: task-specific before copies under output/QA/LeftHandGesture_20261106/before; preserve unrelated working-tree changes.
