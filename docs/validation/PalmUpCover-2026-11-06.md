# Palm-up supporting hand / player covering grip — 2026-11-06

Accepted reference: generated diagram exec-506c66db-17f9-4ebb-85d3-8132bbad97c3.png. Girl offers her right palm facing up and keeps it open; player left palm faces down and covers it, fingers lightly closing; wrist stays continuous with forearm.

Changed:
- ParkGirlPresentation: anatomical palm normal is consistently local -X, instead of choosing whichever side points upward. During OfferWeight, finger direction replaces the authored curl with a relaxed open profile (4/10/16 degrees); removed the obsolete additional joined-hand curl state. Animation restoration outside the offer remains in place.
- FirstPersonHands.Handhold: player fingers close gently (12/35/55 degrees); palm length direction primarily opposes the girl's fingers while retaining the lower-left entry; existing contact-first forearm solve remains.
- LeftHandGestureChecks: actual anatomical palm-up assertion, open distal fingers, player palm above girl's, and real-mesh close-up renders. Existing wrist/root/clearance/state checks retained.

Baseline regression: new open-palm assertion failed on Right_IndexDistal before runtime changes. Front and meadow-relative sequences then passed, including pause/gate/shutter/release/re-entry/cancel and unchanged checkpoint checks. FirstPersonArmChecks passed. Palm normals point up (dot ~1); player palm marker is 26 mm above girl's marker; centre separation 39.1 mm; wrist bend 0–0.02 degrees. These are bone marker measurements, not skin gaps.

Evidence: output/QA/PalmUpCover_20261106/{front,meadow}/, including contact-detail.png, reach-60.png and checks.txt. Before-files are in before/. Visually inspected both contact close-ups. No scene, prefab, material or save changes in this iteration.

Limitations: isolated real-model preview tests, not live ParkScene mouse interaction or exact triangle collision verification. The original low-poly arm mesh and textures are retained, so the result does not have the generated illustration's surface fidelity. Editor compilation was verified; a transient MCP WebSocket reconnect warning occurred, with no C# errors. The editor entered Play mode during validation; synchronous test calls restored global pause/transition/time-scale state and did not stop gameplay.
