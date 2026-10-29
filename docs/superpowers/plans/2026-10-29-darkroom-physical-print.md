# Physical darkroom finishing workflow

Goal: replace print-stage click/timer placeholders with scene-authored negative, contact frame, trays, wash basin and drying clips while preserving existing camera/tank workflow and anchors.

Implemented scope:
- Add DarkroomPrintDefinition, DarkroomPrintSceneBinding and a versioned item-placement state using shared ItemSaveRecord and chapter ID.
- Author additive props beneath the existing HouseScene darkroom worktop; keep all existing Transform values and assets.
- Bind authored colliders to PhotoWorld3D; use Station input context and the existing cancellation/release boundary.
- Drag negative to dry; arrange paper/negative in frame; close lid; hold exposure switch; move paper through developer, stop, fixer, wash and drying clip.
- Decode the original immutable frame files into a private contact sheet only after exposure; progressively reveal on physical paper. Full frame gallery remains locked until final saved drying completion.
- Upgrade unfinished workflow 1/2 records to workflow 3; preserve completed rolls and workflow 0 behavior.
- Persist placements, lid, remaining processing time and interrupted exposure; transient drags are never serialized.

User requested implementation without validation. No compilation, Play, tests or visual checks. Runtime placement, reachability, shader import and old/new save migration still require verification.
