# Darkroom physical print — 2026-10-29

Implementation-only increment; not compiled or run, per request.

## Player sequence

After the existing tank rinse:
1. Drag Negative Strip from the tank area onto Negative Drying Clip. Wait for drying.
2. Drag Contact Paper onto Contact Frame, then drag the dry negative onto it (either placement order is accepted).
3. Click the frame rim to close it. Hold Exposure Switch to expose. Releasing early retains the exposure remaining time; press again to continue.
4. Open the rim. Drag paper from the frame into Developer Tray. The three real exposures gradually appear as a contact sheet.
5. After development, move the paper into Stop Bath Tray, then Fixer Tray, waiting for each process to finish.
6. Move paper into Print Wash Tray and click Print Wash Tap.
7. Move the washed paper onto Photo Drying Clip. Only successful persistence of completed drying unlocks the existing full-frame gallery and chapter completion.

Wrong drops return to the last committed station. While processing, material cannot be removed. Esc cancels a current drag/exposure gesture first; another Esc leaves the station. Safe inspection is not involved.

## Authoring

Hierarchy: existing `Darkroom worktop / DarkroomPrintStation`.

- All added materials, trays, frame, controls and placement anchors are authored in HouseScene, not generated during Play.
- `DarkroomPrintSceneBinding` references props by explicit IDs, their placement anchors, paper renderer, frame hinge and indicators.
- `Assets/Data/Photography/DarkroomPrint.asset` controls timings and forgiving drop tolerance. Values are whitebox durations, not a chemistry simulation.
- `Assets/Data/Chapters/summer.photography.asset` references the print configuration. The scene binding also directly references it for initialization before chapter registration.
- New items use `ItemSceneBinding`, type definitions and stable instance IDs. Existing models, scene positioning objects and transforms are retained; only the worktop child list and photography item/configuration lists are extended.
- WorldHotspot metadata is attached to the authored colliders at initialization. No replacement geometry or layout is created by that binding step.
- New labels use existing WorldTooltipLabel; no TextMesh labels were added.

## Persistence and recovery

ParkDarkroomProgress workflowVersion 3 adds `DarkroomPrintState`: frameClosed and two common ItemSaveRecords. Their location IDs reference scene anchors, never saved world coordinates.

Active tray/drying timers persist remaining time and resume only while the chapter is running. Offscreen time is not counted. Exposure persists as ContactReady plus remaining time; held input never resumes automatically. Drag positions are not saved: re-entry places the material at its last valid location. A new scene layout therefore uses its current anchors.

Unfinished workflow 1/2 rolls upgrade in memory, mapping old phases to sensible material positions without repeating completed chemistry. Already developed rolls retain gallery access. Workflow 0 remains on its earlier route. Original exposure files are not overwritten or recaptured.

The contact sheet renderer uses the exact saved frame images, aspect-fitted into three cells. It is a development preview on physical paper, not an early full-photo gallery. Shader reveal is visual only; completion is decided by the persisted process state.

## Contextual guidance increment

The existing progress line now names the next physical action throughout rewind, bag loading, tank processing and paper printing. No extra task panel or scene label is created. During timed processes it shows a short status and remaining seconds.

WorldTooltipPresenter uses the chapter's actual Pick ray when the darkroom is active. Contextual English tooltips replace stacked static labels there: object name plus action, drop instruction, waiting state or a short indication that the object is not needed yet. Film chemicals and paper trays have distinct names.

Only the next actionable source receives a subtle warm tint; while dragging, the tint transfers to its destination. Waiting phases have no actionable highlight. The tint is removed on station exit, input-context changes, presenter disable and chapter disposal. Property blocks preserve the contact sheet texture and reveal value; no shared materials, geometry, anchors or save payloads are changed. Completed prints are highlighted for reopening the gallery.

Implementation only: compilation, Play, hover reachability, material appearance and exit/re-entry checks have not been run.

## Pending verification checklist

- Compile/import components, materials and shader.
- Confirm tray/frame/clip reachability and overlap against the current user-edited room.
- Cancel each drag and exposure, exit during each timed phase, then resume/reload.
- Recover old workflow checkpoints, especially active developer/fixer/drying stages.
- Wrong tray, premature removal, closed-frame pickup and missing references.
- Final save failure must retain negatives and block gallery/chapter success until retry.

Offline authoring tool: Tools/Migrations/author_darkroom_print.py. It does not run Unity. Existing authored stations are left untouched on rerun.
