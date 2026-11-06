# Brush transitions — 2026-11-06

## Implemented behavior

All first-party single-scene navigation now uses `BrushSceneTransition`. It renders the imported EasyTransitions BrushIN and BrushOUT prefabs on a persistent overlay. IN completes before scene replacement; the covered state lasts as long as loading and initialization require. OUT starts from its first frame after readiness and an additional frame. Animation uses unscaled elapsed time, so pause does not freeze it. A transparent full-screen graphic and shared gameplay input gate prevent interaction while covered.

Routes covered: main-menu new/continue; pause-menu return; House/Park entry and all Park exit paths; spring/summer/autumn/winter bedroom changes; final departure room; bedroom failure retry; debug routes that load bedrooms. Existing camera/station movements within a scene remain camera movements.

`WBMain` still owns additive bedroom loading, binding, saves and physics/camera setup. Its new readiness property waits for bedroom binding, including debug entry, before the outer navigation reveals. Readiness uses the requested host path because bedrooms deliberately become Unity's active scene. Single-scene departure drains any summer preload held at an activation barrier. Locked Park entry can redirect to House without revealing the rejected destination.

Initialization errors retain an opaque cover with Retry/Main Menu actions. Main-menu actions explicitly reject keyboard Submit during Brush as well as pointer input. The imported vendor files and authored scenes were not edited by this task. A Resources copy of Brush settings ensures the two imported prefabs are included in player builds; no Inspector setup is required.

## Evidence

- Initial API existence check failed before controller implementation.
- Unity Editor compiled the initial integration and `BrushTransitionChecks.AssetsAndRoutes()` returned PASS; Console query returned no error/warning entries at that point.
- Independent code review found keyboard Submit could archive saves or strand the menu while the transition was busy. Added entry guards before those side effects. Static regression changed from FAIL to PASS.
- Final `python Tools/verify_brush_transitions.py`: 18 PASS assertions, covering asset GUIDs, non-looping clips, all first-party scene-load owners, menu guards, readiness and preload/reveal order.
- Final runtime and Editor C# compilation used Unity 2022.3.62f3's bundled `NetCoreRuntime/dotnet.exe` and `DotNetSdkRoslyn/csc.dll`, with copies of the Editor's response files. Only output/refout paths and the Editor's runtime-assembly reference were redirected to `output/validation/brush-transitions`. Both compiler processes exited 0 and diagnostic logs were empty. The generated DLLs exist in that isolated directory. This did not request Editor compilation or a build.
- Scoped `git diff --check` passed. Existing unrelated uncommitted changes remain in the checkout; no mixed-work commit was created.

## Validation limits and cleanup

Play testing was interrupted by concurrent Editor activity before its first animation assertion. It only recorded the asset audit. The user then explicitly requested static checking only. Pause timing, visual coverage at different aspect ratios, initialization-error UI rendering, and full end-to-end scene traversal therefore remain runtime acceptance checks, not passing claims. The opt-in Editor runner `BrushTransitionChecks.StartRuntime()` is retained for a future isolated Play session.

The test temporarily set the Play start scene to MainMenuScene; that override was restored without changing the current scene or Play state. Tests used the EditorPlaySession slot, with its pre-test contents backed up to `Backups/BrushTransitions-20261106/EditorPlaySession-original`. That backup was deliberately not restored over another task's current test saves. Production saves were not used.

The imported Brush animation defaults to approximately one second for IN and one second for OUT. Loading holds are variable; they do not simulate a percentage and do not count scene-load stalls as elapsed time at the start of OUT.
