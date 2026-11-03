# Winter Album E implementation plan

Goal: user-authorized E01–E07 independent editable art, Unity imports/prefabs, deterministic visual interfaces and isolated scene/screenshots/GIFs. No game integration or gameplay changes. User explicitly requests direct execution; current checkout and separate new art/output directories preserve existing concurrent work. No commits/push/resets.

## State map established before modeling
| Existing action | Current state | Asset/node | Presentation |
|---|---|---|---|
| Raise/lower album at screen edge | BookOpen, Phase | E01 covers/pages, fixed WorldPresentationAnchor | open/close + sampleable turn; no state changes |
| World image | 1280×720 RT from camera at (8,2,-16), ortho size5.2, view XY from -Z | E01 left/right content materials | independent MPB UV crops across spread; optional single-page fit; no mirrored U |
| A/D, Space movement | Actor, Velocity, Grounded | E02 one skeleton, foot/palm/seat anchors | in-place idle/walk/turn/takeoff/air/land/push/sit/dismount/carrier/key reach |
| Mount/pump/jump swing | Riding=0, SwingAngle/SwingSpeed, max65deg, SwingSeat=(4.5+3sin(a),3.3−3cos(a)) | E03 top SwingPivot, rigid-length twin ropes and seat | Unity Z rotation +angle; grip/seat remain attached |
| Observed bridge appears | Painted[1], BridgeGrowth moves0→1 in.65s; Floor admits only>=1 | E04 fixed3-unit deck, endpointsx9/12 y.7 | material reveal along X; no invented drawbridge/whole scaling or collision toggle |
| Push suitcase / step on plate | CrateX; PlatePressed includes crate OR actor | E05 plate, latch, gate, counterweight, cable, walkway | unified load fraction; gate centreY1→3.2, link changes; caller owns boolean/smoothing |
| Ride automatically rotating flower | FlowerAngle radians, Riding1..6; radius2, hub(8,2) | E06 hub and six level carriers | unbounded angle; child carriers counterrotate to stay horizontal |
| Drag pendant into recess | AtDoor, Painted[4], KeyInserted; release match threshold.55 | E07 actual D02 silhouette recess, insert/seat points, hinge/bolt, nested existing key | externally set insertion/open/visibility; one key representation only |
| Restore/respawn | restore bridge=Painted[1]?1:0, SwingAngle=-.55, FlowerAngle=-pi/2, storedcrate/key/checkpoints | all visual state setters | explicit restore caller state, not new persistence |

E01 real book closed240×320mm. E02–E07 use ONE source-to-game coordinate factor0.1 (miniature source); a common AlbumWorldAdapter can use scale10 to map to existing logical route. E01 content/world alignment separate, never deform world with page. Existing D02 pendant fits 34mm source recess directly; D04 painted suitcase referenced as source prop with documented miniature conversion, not overwritten. Main display direction -Z for route assets. No ward/ending figures/environment.

- [x] Baseline and xlsx/design review.
- [x] Build original E meshes/PBR, reuse D02 key/D04 case, skeletal E02 source/action export.
- [x] Deterministic presentation-only components; meaningful prewritten recovery/connection tests.
- [x] Actual Unity import, clips, normalized pivots/skin binding, prefabs and independent preview.
- [x] Inspect front/back/bottom and sample states, RT UV, skeleton and mechanical endpoints; record GIFs.
- [x] Independent read-only review, fix findings, manifests/export validation/docs/package.

Review focus: dynamic page aspect/UV and turn ownership; real skeleton/foot contacts with no root motion; swing length and sign; bridge show state separate from passage; constant cable endpoints/length; flower platforms remain level for multi-turn/reverse; exact existing D02 shape and single-key visibility; immutable existing files.

Prior design.html drawing hand/scarf scenario is stale vs current v3 code. Current user request + v3 actual state supersede it. No redesign of state machine. Asset authoring source scripts and validation serve as retained execution ledger.

2026-11-02 final scope update: user deferred all humanoid action/contact fitting. E02 remains a prototype rig with temporary clips. Prop validation passed 806 import +121 driver +32 mechanical checks. Initial independent review completed; final follow-up reviewer was unavailable due usage limit, so no second review is claimed. Final .blend files, manifests, actual Unity GIFs and notes delivered.
