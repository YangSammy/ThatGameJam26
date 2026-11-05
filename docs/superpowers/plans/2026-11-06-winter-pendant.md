# Winter pendant discovery and door framing

Approved design: user confirmed the pendant pocket, same-contour recess, physical page-edge key, once-only discovery cue and approach camera proposal in this conversation.

Goal: make the existing winter pendant puzzle legible without changing chapter/save ownership or movement parameters.

Execution: inline; use existing Unity feature workflow. No new input framework, save fields, source model edits or native mouse testing.

- [ ] Author a half-open pocket on the existing observed suitcase, expose pendant upper contour/chain, preserve fixed observer position and existing scene anchors. Author page-edge and close-camera settings on WinterAlbumWorkspace.
- [ ] Smooth only the route camera toward the door on approach (0.75 s, hysteresis); preserve while book is down and lock during drag. Restore framing from existing high checkpoint/actor state.
- [ ] Move the draggable pendant to the paper layer with separate real-space picking; map drop back through the current album camera and share validity with the recess outline. Preserve cancellation, unique ownership and accepted insertion animation.
- [ ] Restrict once-only chain cue to closed-book room observation; hide the observed key after discovery; improve per-instance key/door material contrast.
- [ ] Compile; run production pointer-handler, framing, cancellation and checkpoint restoration checks; capture actual Play Mode screenshots. Restore original saves/editor state. Document limitations and manual checks.
