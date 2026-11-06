# Brush scene transitions implementation plan

> Use executing-plans inline; the user explicitly approved direct implementation of the smooth Brush approach.

**Goal:** Every player-facing scene change uses the imported Brush, holds coverage until scene and object initialization finish, then reveals.

**Architecture:** A persistent presentation-only BrushSceneTransition uses the existing EasyTransition Brush prefabs/settings. Single-scene navigation is centralized; WBMain retains ownership of additive bedroom binding, preload draining, save and error recovery. No vendor source or authored scenes are modified.

**Tech Stack:** Unity 2022.3.62f3, Built-in, uGUI, C# coroutines.

**Approved spec:** User message on 2026-11-06: smooth Brush, all level changes, reveal only after new scene and objects initialize. Strict percentage synchronization was rejected.

## Constraints and review focus
- Work in the current dev-Nicole checkout containing the imported package and ongoing user changes; preserve all existing edits. No automatic commit of mixed work.
- No new packages; use project-style Editor validation, since NUnit is not installed.
- Single loads must drain held bedroom preloads before issuing another async operation.
- Additive sceneLoaded events must never trigger reveal; readiness is explicit.
- Animation uses unscaled time and no load-frame delta at the start of reveal.
- Block gameplay and duplicate navigation throughout; errors remain covered with actionable retry/menu UI.
- Keep save locations isolated during runtime checks; restore the original editor scene setup.

## Tasks
- [x] 1. Add asset/API checks; demonstrate failure before implementation. Add persistent Brush presentation and resource configuration, plus a single-scene loader with readiness/error handling.
- [x] 2. Route menu, park/house, bedroom/retry/debug scene paths through Brush. Preserve background preloading without showing Brush until actual navigation.
- [x] 3. Verify compilation, asset coverage and navigation ownership; inspect changes and document results. Runtime acceptance was explicitly removed from this session by the user because another task owns the Editor.

## Execution ledger
- Baseline: Unity connected, Edit mode, clean Bedroom_Autumn scene; no compile errors returned, one existing MCP websocket warning. The repository already has extensive uncommitted work.
- Existing project context inspected: docs/AI/UnityProjectContext.md. Build scenes now include MainMenuScene, HouseScene, ParkScene and four V17 bedrooms.
- Task 1 RED: dynamic check reported missing BrushSceneTransition before implementation. After import, AssetsAndRoutes passed and Console returned no errors/warnings.
- Task 2: centralized all first-party single-scene loads; additive binding/preloading retains its existing owner. Bedroom active-scene changes cannot bypass the requested host's readiness check.
- Review: independent reviewer identified keyboard Submit reaching StartGame while Brush is busy. Reproduced as missing entry guard before save/archive; fixed NewGame, Continue, StartGame and other menu actions. Static regression checks now pass.
- User requested static checks only after another task interrupted Play Mode and changed the scene. No further Play/scene changes performed. Restored only this test's playModeStartScene override, conditional on it still matching our temporary setting.
- Task 3: Tools/verify_brush_transitions.py passes 18 assertions. Unity's bundled Roslyn compiled current runtime and Editor sources independently into output/validation/brush-transitions, both exit 0 with empty diagnostic logs. No build or complete Play/visual pass claimed.
- Test save backup retained at Backups/BrushTransitions-20261106/EditorPlaySession-original. Do not restore it over another task's live saves. No production save slot was used.
