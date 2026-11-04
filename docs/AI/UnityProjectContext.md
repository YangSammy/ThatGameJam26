# Unity project context — asset authoring inspection

> 2026-11-03: The user approved aligning the full game to the v3 PDF. The historical snapshot below is not the current design contract. Read [V3 alignment plan and ledger](../superpowers/plans/2026-11-03-v3-alignment.md) for accepted differences, implementation order and verification status. Existing assets and persistent artifacts remain authoritative; do not rebuild them from this snapshot.

> 2026-11-03 photography/darkroom update: Park now owns the only mechanical loading sequence; House enters the memory first and preserves loading state read-only. Three resume anchors and physical chemical/tray cues are authored. Read [execution and verification report](../validation/V3PhotographyDarkroom-2026-11-03.md) before using historical camera entry/testing instructions. Full native gesture and character-visual acceptance remain open.

<!-- unity-onboarding:generated:start -->
- Inspected 2026-10-31, commit `00bf2c64648733bc7d226844e7a52d35bd5fd41f` with existing uncommitted changes.
- Root: `D:/GameJams/ThatGamejam2026`; Unity 2022.3.62f3 (96770f904ca7), Built-in (`GraphicsSettings.m_CustomRenderPipeline=0`), legacy input (`activeInputHandler=0`). Confirmed from project settings.
- Enabled build order: HouseScene, ParkScene. `WBMain` owns house chapters; `PhotoWorld3D` builds mechanical camera geometry at runtime. Park uses its own `ParkPhotoPrototype.Controls` instrument geometry. Both are existing systems, not interchangeable model references.
- Main runtime code: `Assets/Scripts/{Core,House,Player,Interaction,Park,Chapters}`; Editor authoring and checks: `Assets/Editor`. Asset source/output convention: `output/`, runtime art: `Assets/Art`. See `docs/ProjectOrganization.md`.
- First-party C# uses `MemoryWhitebox`, MonoBehaviours plus ordinary chapter/controller classes and partial files. No first-party asmdef found in current inspection. Input/interaction routing and scene anchors must remain intact.
- Packages include built-in modules, uGUI and Coplay Unity MCP v10.3.0. No SRP, new input package or networking dependency in manifest. No Unity Test Framework dependency; existing validation is project-specific Editor checks under `Assets/Editor/Validation`. Build entry is `Assets/Editor/Build/GameBuild.cs`.
- Camera IDs include `camera-body`, `finder`, `chamber`, `spool`, `leader`, `back`, `rewind`, `advance`, `shutter`, `shutter-dial`. House procedural body is 0.78 x 0.42 x 0.32 m; Park body is 0.36 x 0.22 x 0.075 m. New real-size 0.14 m asset cannot be dropped over those colliders without a separate approved adapter.
- Existing authored entry/inspection camera in HouseScene uses `Camera - photo chapter`, `Camera body`, `Back inspection hinge`, `Shutter inspection button`, and primitive chamber parts. SafeInspection builds a display-only copy. Preserve those names and references.
- Photo state restoration is owned by `PhotoWorld3D.Recovery`, `PhotoProgressStore`, `ParkFilmStore`. `Photo.FilmThreaded`, phase, settings, rewindTurns and backOpen are restored independently of model presentation. Sample art must not read/write saves or infer gameplay completion.
- Available tooling: live Unity MCP connected to this project, read-only execute and Console checked; active scene ParkScene, clean, Edit mode. Blender 3.6.23 is available. Author new model in a separate background Blender process to preserve the user's open Blender work.
- Baseline Console: zero warning/error entries returned. Existing modified ParkScene and scripts are unrelated work; no resets, scene saves or broad repairs authorized.
- Unknown: final integration scale/camera framing and hand fitting for the new asset; those are outside the first sample. End-to-end gameplay not validated by this authoring inspection.
- Evidence: ProjectVersion.txt, GraphicsSettings.asset, ProjectSettings.asset, EditorBuildSettings.asset, Packages/manifest.json, README.md, docs/ProjectOrganization.md, PhotoWorld3D.cs, PhotoWorld3D.Recovery.cs, PhotoProgressStore.cs, ParkPhotoPrototype.Controls.cs, SafeInspectionBuild.cs, serialized HouseScene names and Unity live state.
<!-- unity-onboarding:generated:end -->
