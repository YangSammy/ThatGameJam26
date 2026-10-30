# Physical interaction contract

## Operation areas — follow-up implementation

HouseScene now contains an `OperationArea` component on each existing table:

- `Shared writing and recording desk`: `house.study.work-area`, three original
  cassettes, three work cassettes and the detached adhesive patch.
- `Darkroom worktop`: `house.darkroom.work-area`, tank, three chemical bottles,
  negative and contact paper.

Existing generated ISO film props register with the study area; the exposed film
registers with the darkroom area. No extra model or operation camera is generated.

Inspector fields: `Center` and `Size` describe the rectangle in the table's local
X/Z coordinates; `Surface Height` is local Y. The current cube worktops use
height 0.5 and size 0.98 by 0.98, leaving a small edge margin. Scaling or moving
the table moves the area with it. Select the table to see its boundary gizmo.
`Members` explicitly assigns objects and stable IDs; being visually close to a
different table does not transfer ownership. Existing ItemSceneBinding IDs are
reused where present. New draggable props need a member entry in their intended
area and their existing gameplay drag entry point.

Ordinary movement uses the area plane. Bounds include the full rendered object,
so the center cannot be moved far enough to leave half the object over an edge.
An edge clamps the object instead of cancelling the drag. Releasing at a clear
table position keeps the item there. Occupied positions reject placement; there
is no stacking or pushing other objects. Esc still restores the pre-drag pose.
Lift is based on the initial supporting height so an elevated prop does not drop
to the table on pickup.

Allowed gameplay receivers override the plane/boundary during approach. These
remain explicitly selected by each chapter: A/B socket, camera chamber, dark bag,
tank, drain basin, contact frame, chemical tray or drying clip. Crossing another
table is not a general exception. Merely placing paper or tools on a table never
advances chemistry or exposure. A detached adhesive patch can rest on the desk;
the uncut strip remains constrained to its dispenser gesture.

Five-day saves have an optional `areaPlacements` field containing stable item and
area IDs plus area-local position/rotation. Only committed placements enter it;
moving objects are not sampled by periodic saves. Old saves default to authored
placements. On restore, placement bounds are recalculated against the current
area. Existing chapter state remains authoritative for inserted/consumed items.
Print material records use `area/<areaId>` when resting freely; missing placement
data falls back to its original home. Existing per-chapter save files and the
five-day file still commit separately; this does not introduce a transaction
across those files.

This follow-up was implemented without compilation or Play validation. Deferred
checks: edge/corner dragging with large cassettes, occupied drops, Esc, reload,
selecting a previously placed cassette, and all receiver exceptions. Bounds are
conservative and may need scene-specific adjustment around oversized colliders.

Implementation pass: 2026-10-29. Compilation, automated tests and Play Mode were not run, as requested.

## Shared drag session

`Assets/Scripts/Interaction/ObjectDragSession.cs` owns temporary motion, not chapter progress.
It captures the starting world pose, local scale and pointer grab offset. The drag plane is
chosen at press time and stays fixed until release. It uses renderer bounds and a box sweep
to stop translation before scene colliders. Trigger volumes and the existing invisible
walking footprints do not act as physical walls. The currently accepted receiver is exempt
from the sweep so its interaction volume does not prevent insertion or pouring.

| Interaction | Motion | Release / cancellation |
| --- | --- | --- |
| Darkroom bottle and tank | Horizontal plane, 4.5 cm lift; existing receiver-specific tilt | Valid receiver performs the existing action; tool returns to its authored home |
| Negative and paper | View plane, 2.5 cm lift | Valid receiver updates saved location; invalid drop restores the previous stable placement |
| Original cassette and work cassette | View plane, preserved grab offset, 2.5 cm lift | Visible A/B socket accepts; otherwise existing return placement |
| Adhesive head | View plane, up to 45 cm pull | Crossing the cutter tears a patch; releasing early cancels the pull |
| Detached adhesive patch | View plane | Existing visible, facing protection-hole rule commits; invalid release returns to strip rest |
| Cassette reels | Actual selected reel, LMB + wheel | Shared tape-position model unchanged; shell rotation cannot take over an active reel gesture |
| Cassette shell rotation | LMB + RMB chord | Releasing either button ends rotation and waits for both buttons to release |
| Camera / bag mechanical controls | Existing bounded wheel or hold action | Carrying another object cannot also operate a camera dial |
| Writing and painting | Existing writing surface / gaze mechanic | Shared cancel contract; committed ink and chapter progress remain |

`DragReceiverProbe` shares visibility, tolerance and reach between darkroom hover and drop,
and between cassette slot hover and drop. Darkroom retains its definition's `dropTolerance`
and 2.5 m reach. Cassette slots use 4.5 cm tolerance and 3 m reach. Translation defaults to
1.8 m from the grab origin. These are code defaults; no scene migration or new component
attachment is needed. Existing Inspector placement anchors remain authoritative.

## Input and persistence

- A gesture keeps its initial action; pressing an unrelated button cannot repurpose it.
- Only release of LMB ends a LMB operation. RMB no longer resets or cancels cassette handling.
- Esc first cancels an active gesture. Subsequent Esc follows the existing context exit/pause rule.
- `WorldChapter3D.HasPointerGesture` and `CancelPointerGesture` give writing, painting and
  photography the same host cancellation path. Focus loss, pause and transitions cancel rather
  than treating the current cursor position as a successful drop.
- The existing input context release gate prevents held buttons from starting a new action
  after a cancellation or context transition.
- The drag session is transient and is never serialized. Controllers continue to own stable
  location IDs, chemistry stages, reel checks, attached patches and recording progress.

## Feedback and remaining limits

Darkroom and cassette movement use short English blocked-path / release / cancel hints.
Darkroom continues to highlight the current receiving object. Existing assets, scene transforms,
operation cameras and save schema are unchanged.

The sweep is a conservative axis-aligned visual-bounds approximation, not a physics grasp or
an authored collision hull. Receiver placement animations and free rotation are not swept.
Existing oversized interaction colliders or objects already overlapping at rest can still
cause blocked motion; these require a later scene-level pass. No full collision-free guarantee
is made for final receiver poses or animations.

## Deferred manual checks

1. Grab bottles/tank at an edge; verify no pointer-centering jump and try moving around a neighbor.
2. Pour/drain over the designated receiver, then try an obstructed or invalid drop.
3. Move paper through trays/frame/clips; Esc, pause and focus loss must restore a stable placement.
4. Load A and B; release away from each slot; verify RMB does not eject/reset a cassette.
5. Pull across the cutter, cover each visible hole, cancel before/after tearing, and resume.
6. Hold a reel, press RMB, release RMB first; no shell rotation or premature commit.
7. Write/focus while holding LMB; release RMB, cancel with Esc, then restart only after releasing input.
8. Resume each chapter from its existing save and verify no intermediate drag pose was stored.
