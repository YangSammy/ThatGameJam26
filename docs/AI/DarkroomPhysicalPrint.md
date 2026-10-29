# Darkroom physical print — 2026-10-29

Implementation-only increment; not compiled or run, per request.

## Continuous darkroom interaction

An exposed park roll now exposes the existing physical photographic controls directly in HouseScene. Approach and aim at a reachable control: the cursor becomes available without station entry or a camera transition. WASD moves in the room; hold RMB to look; LMB operates the object. Movement/look are held during an active LMB gesture. Walking beyond the working reach cancels the local context and saves progress, returning normal exploration controls. Timed work pauses when not using the workspace. The loading-camera station remains for preparing a new roll; this change does not convert writing or painting.

Esc first cancels a held action; otherwise it pauses. It does not teleport back or exit a chapter. Gallery Esc closes the gallery. Final print completion shows a short ready message and leaves prints hanging; click them to view. Photograph chapter completion no longer schedules an automatic station exit. Park returns and the House `2` shortcut prepare the workspace and open the curtain without entering a station. Existing exposed film still needs rewinding and removal at the camera; its controls are accessible directly, then continue at the dark bag.

Tank processing for a park roll advances negative chemistry without calling the old single-photo completion evaluator. Only persisted final prints mark the roll developed. The same stored stages, item locations and remaining-time fields are retained.

Receiving regions now use expanded receiver bounds along the cursor ray, not a plane through the target centre. Invisible walking footprints do not obstruct tabletop picking/drops; visible geometry still does. Valid tank/bottle drops preview above the receiver with clearance based on the tool renderer bounds and tilt, then commit on release. This is controlled placement, not a general Rigidbody collision/grabbing system: free dragging can still intersect unrelated props and needs further collision work if desired.

No compilation, tests or Play checks were run. Continuous input handoff, room movement, current scene occlusion, target preview and save/resume remain pending runtime validation.

## Inspector pose ownership

Authored darkroom equipment uses its pre-Play Transform as its resting pose. The frame lid now captures its closed rotation instead of resetting to world/local zero. Negative and paper home placement captures their own Transform rather than overwriting edits from duplicate home anchors. Their scale is not changed when transferring between stations. Existing home-anchor objects and stable location IDs are retained for compatibility.

Destination Placement transforms for the frame, trays and drying clips are now children of their receiving object, preserving their current world pose. Moving/rotating a receiver carries its landing point; fine-tune that child to change the landing position/orientation. The materials are not parented to the receiving mesh, so receiver scale does not resize the paper or negative.

Select DarkroomPrintStation / DarkroomPrintSceneBinding to adjust the Motion relative to authored poses section: frame opening rotation, tank lid opening offset, bag working-scale multiplier and grip rotation, tank agitation/draining angles, bottle pouring angles and negative offset above the paper. Values are offsets/multipliers, not replacement resting transforms. Tank lid offset is in parent local units and scales with the authored model. Initial values preserve the current layout and motion.

For initial layout, edit the actual paper/negative/tool Transform; for a later landing pose, edit the receiver's Placement child. The frame lid Transform describes its closed pose; the configured relative rotation describes opening. These are pre-Play authoring settings, not live animation editing. Disposal restores captured material/frame/tool poses without deleting scene objects.

This increment did not run compilation, tests or Play validation.

## Park test shortcut

The same `2` / numpad `2` shortcut now works in HouseScene exploration and its photography station. It saves current photography progress first, uses the shared DebugPhotographyFilm filling service, refreshes camera-loading state after newly completing a roll, opens the curtain and enters the existing photography station. Already full/developed rolls retain their current darkroom state; no new roll replaces them. Save failure cancels the shortcut. Exit unrelated stations/tape interactions before using it; transition, inspection and input-release gates still apply. Normal day availability rules remain in effect. No scene assets or anchors are changed by this shortcut.

In Editor Play mode or a Development Build, press top-row `2` or numpad `2` in ParkScene. The shortcut completes camera loading if necessary, retains committed exposures, and fills only remaining slots with generated colour/silhouette test negatives (white marks indicate frame number). It uses SaveTask and SaveExposure, including sequence/reference/hand checkpoints, then opens the existing Return to darkroom panel. No automatic teleporting or scene transition is required. Existing sealed/developed rolls are retained, including their darkroom progress. Repeating after a partial write failure resumes remaining slots. Release controls before clicking the return button.

This shortcut is compiled out of non-development builds. It does not unlock the gallery early or mark the darkroom complete. No compilation or Play validation was run.

## Interaction reliability increment

- After the negative rinse, click the tank lid to remove the negative. The saved `negativeRemoved` flag controls this step. The strip is presented at the existing `print-negative-home` anchor, now beside the tank rather than inside it. Old saves already hanging/contact-printing the negative infer removal automatically; saves at the pre-drying home start with the lid action.
- Chemical pouring, basin draining and physical-paper drops use a common target-region check (reach limit, collider tolerance, unobstructed path). Pour/tilt previews and release acceptance share that check. The drain basin now has its own collider; the tap is only a rinse control. Hover confirms `Release LMB here` only when acceptance is valid.
- All held left-button gestures ignore right-button release. Right-button looking cannot begin mid-gesture. Chemical/tank dragging retains its original grab offset.
- Short English failure messages cover closed frame, wrong bottle, unsealed tank, pending drainage, processing and rejected drops. Rewind/bag percentages and remaining exposure are visible. Re-entry identifies restored progress beside the current task.
- House station exit now stops if stable saving fails; it retains the current chapter and prompts an Esc retry. This is a retry of persistence, not a reset of the roll. Autosaving can also retry while the station stays active.
- Negative/paper/clip/switch/frame-rim hit volumes have been modestly enlarged without replacing visible meshes. Original scene IDs and bindings remain. The only relocated placement is the existing negative extraction home.

No compile, tests or Play checks were run for this increment. Reach/occlusion feel, save-error recovery and all exit stages still require runtime validation.

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

### Workflow layout increment

The existing worktop, player/camera anchors and prop references remain in place. DarkroomPrintStation now contains three English hierarchy groups:

- Film Processing: compact dark bag at the near edge, tank beside it, film chemicals and rinse/drain equipment behind, negative drying clip toward the back.
- Contact Printing: paper supply in front of the frame, exposure control to its side, unobstructed frame opening space.
- Paper Processing and Drying: developer and stop bath in the near row, fixer and wash tray in the far row, photo clip and rail at the back. Hanging negatives and prints face the accessible side of the counter.

Existing placement anchors move with their corresponding objects; saved location IDs remain unchanged. The older generated film equipment now reads added `film-* Anchor` positions from DarkroomPrintSceneBinding. Those anchors control initialization positions, not replacement meshes. Bottle caps now travel with their bottles. Drag return locations and pour endpoints use the relocated equipment. No player/camera anchors or room collision/layout volumes were changed.

Layout authoring: Tools/Migrations/layout_darkroom_workflow.py, one-shot and non-destructive on repeat. This layout has not been imported, compiled or Play-tested.

### Runtime checks still pending

### Authored film equipment increment

HouseScene now owns the dark bag (body, sleeve, grip), developing tank and lid, three chemical bottles with caps, rinse tap/stem, drain basin and timer. Find these directly under `Darkroom worktop / DarkroomPrintStation / Film Processing`. Their original `film-* Anchor` transforms remain present.

Adjust the equipment root Transform in Inspector for position, rotation and scale. Adjust `Tank Lid` and `Bag Grip` children for their resting poses. On initialization the controller captures those actual scene poses; it does not copy the old anchor coordinates onto the equipment. Drag return, tank agitation, bag motion and lid opening use the captured baseline. The frame/timer/printing references are still provided by DarkroomPrintSceneBinding; each tool also has its existing-style ItemSceneBinding and tooltip data. No additional manual wiring is required by this migration.

HouseScene no longer calls the procedural equipment builder, including when a reference is missing (a missing reference is reported instead of spawning duplicates). Other legacy scene paths retain their original fallback builder. The liquid stream remains a transient runtime visual effect; the tools themselves are serialized scene objects. Scene tools are restored rather than destroyed when the chapter is disposed.

Authoring script: Tools/Migrations/author_darkroom_film_equipment.py. Implementation only; no compilation, import checks or Play validation performed.

- Compile/import components, materials and shader.
- Confirm tray/frame/clip reachability and overlap against the current user-edited room.
- Cancel each drag and exposure, exit during each timed phase, then resume/reload.
- Recover old workflow checkpoints, especially active developer/fixer/drying stages.
- Wrong tray, premature removal, closed-frame pickup and missing references.
- Final save failure must retain negatives and block gallery/chapter success until retry.

Offline authoring tool: Tools/Migrations/author_darkroom_print.py. It does not run Unity. Existing authored stations are left untouched on rerun.
