# Autumn farewell implementation plan

Approved design: looping idle; stationary wave; turn; walk out; EasyTransition to winter.

Goal: finish only after the girl's departure, retaining existing story save ownership.
Architecture: WritingChapter owns one timeline. WritingWorld3D samples placement and the existing rig clips from it. WBMain starts the existing bedroom transition directly after committed completion, cleaning the writing view under cover.
Tech stack: Unity 2022.3, existing transform animation sampler, EasyTransitions Brush.

- [x] Pin no-early-completion with a failing regression.
- [x] Add shared wave/turn/walk timing and rig walk binding; preserve idle and checkpoint recovery.
- [x] Cover the farewell view before leaving the station, then load winter through existing ChangeBedroom.
- [x] Run 30/60/120 Hz rules, rig sampling/position checks, compilation and diff checks.

Constraints: preserve user saves and unrelated changes; do not replace the running Player build. Use existing checkout.
Review focus: full-row farewell regression; reloaded farewell starts safely at arrival; timestep spikes; empty animation bindings; cover before cleanup.

