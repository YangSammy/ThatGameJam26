# Park route boundary — 2026-11-05

## Authored changes

- `Assets/Scenes/ParkScene.unity`: added `Environment/RouteBoundary`, containing 32 fence panels and 54 hedge strips. Fence panels have solid BoxColliders. Existing bridge rails close the bridge section.
- `Assets/Editor/Park/ParkRouteBoundaryAuthoring.cs`: one-time authoring and boundary validation helper. No runtime generation, automatic rebuild, movement rules or save changes.
- Reused existing park fence and Hedge_SoftStrip prefabs; all new objects are editable in the scene. Collider is on each Fence node; visual sizing is isolated under ModelFit.
- Entrance/path lead to the bridge and meadow; wider photography pockets retain the existing reference positions.

## Evidence

Evidence and scene backup: `output/QA/ParkRouteBoundary_20261105/`.

- 930 perimeter ray samples: zero gaps.
- Spawn, companion route waypoints and three reference cameras: clear of new collision.
- Route segment centerlines: zero crossings of new walls.
- Actual Play Mode CharacterController.Move against each new fence: 32/32 blocked on the interior side (`controller-collision.txt`). These are scripted controller checks, not a native keyboard walkthrough.
- Play Mode screenshots: `01_Entrance.png`, `02_Bridge.png`, `03_Meadow.png`. Bridge/meadow views were staged using the player camera; screenshots do not imply photography objectives were completed.
- Console after testing: zero error/warning entries returned. No full photography or save-recovery regression performed for this scenery change.
- Scene block comparison: 550 new blocks, no deleted blocks, one existing Environment child-list block updated; no existing local position/rotation/scale changes (`scene-diff.json`).
- Original EditorPlaySession restored: four files, zero hash mismatches. Restored Edit mode, clean HouseScene and original null play-start override.

## Manual follow-up

Walk entrance → bridge → meadow; push against bends and bridge joins. Verify WASD position adjustments at all three photo tasks, girl movement, and return route. Existing trees/props outside this task may still have unrelated collision issues.

## Revert

Remove only `Environment/RouteBoundary` to undo the new enclosure; existing scene objects and bridge rails were retained. The saved before-scene is for comparison/recovery, not for overwriting subsequent unrelated edits.
