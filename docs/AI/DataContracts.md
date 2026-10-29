# Data contracts — 2026-10-29

Implementation only. Compilation, Play, automated tests and runtime migration tests were not run, as requested.

## Ownership

| Concern | Owner |
| --- | --- |
| Stable chapter identity | ChapterIds (five explicit strings; ordering is an adapter only) |
| Immutable chapter configuration | ChapterDefinition; existing tape commission assets are referenced, not copied |
| Immutable item type | ItemDefinition |
| Authored item identity/model/home/colliders | ItemSceneBinding |
| Authored chapter root/operation view/item list | ChapterSceneBinding |
| Gameplay state | Existing Chapter subclass and chapter controllers |
| Transient drag/held input | Existing interaction controller; never part of item snapshots |
| Chapter save envelope | ChapterSaveRecord; opaque versioned payload owned by the chapter |
| Item save envelope | ItemSaveRecord; location ID and chapter-specific payload |
| Durable produced audio/images | Existing PCM/film stores; not embedded in the global save |

HouseScene contains additive bindings for 15 existing objects and five chapters. ParkScene references the same photography definition. No Transform, mesh, collider, anchor or controller reference is replaced. `WBMain.FindItem`, `FindChapter` and `FindChapterBinding` expose registered objects. Existing runtime-created WorldHotspot controls expose a chapter-qualified InstanceId; they remain owned by their chapter rather than being duplicated into an independent inventory. Their IDs must be unique within that chapter if used for persistence.

Definitions live in Assets/Data/Items and Assets/Data/Chapters. Item capability fields are descriptive configuration for new adapters; existing gesture controllers remain authoritative in this increment. No generic inventory or new hand-held state is introduced. Existing tuning fields remain where they are unless their controller explicitly consumes a definition asset. This is an adapter migration, not a rewrite of all gameplay state classes.

## Authoring

- Renaming or regrouping an object must not change instanceId.
- Duplicating a persistent object requires a new instanceId (component context menu: Assign new instance ID). Startup rejects duplicates in the house registry.
- Replacing the model preserves identity and updates model/hitSurfaces references. Fixed objects are never moved by ItemSceneBinding.
- Configurations cannot contain live scene references. Store anchors on bindings.
- Existing work tape IDs are tape.work.1/2/3. Their definition ID is tape.work. Keep these when migrating old saves.
- Slot IDs tape.slot.b, tape.workbench and tape.home express placement. Existing controller anchors remain responsible for actual transforms.
- Do not modify a ScriptableObject to store player progress.

## Save compatibility

FiveDayProgress keeps its existing file path and backup behavior. Version 1 positional arrays upgrade in memory to version 2 ID-addressed ChapterSaveRecords. The next successful write stores records; old arrays become null compatibility fields. Record schema and chapter payload schema are independent. Unknown/duplicate IDs block loading rather than silently resetting progress.

Tape checkpoint version 8 stores the three work tapes as ItemSaveRecords with stable instance IDs and preparation payloads. Version 1–7 remain readable through the historical tape IDs. B/workbench/home locations replace persisted selection indices. Temporary selected/held input is still cleared. Other chapters retain their existing versioned payloads inside the common envelope; their v3 gameplay rewrite will introduce their expanded domain states separately.

Photography stores now explicitly identify summer.photography. Existing film IDs, frame IDs, files, capacities and legacy workflow migrations are unchanged. The film manifest remains authoritative for camera loading, task responses and darkroom progress.

## Submission ordering

1. Stop transport and write the current PCM file.
2. Evaluate acceptance without modifying samples.
3. Build a detached next RadioTapeChapter snapshot.
4. Commit the next snapshot and audio artifact reference in the same story write.
5. Only after success, apply it to the live chapter, change decks and display success.

Failures before step 4 leave the live commission/preparation unchanged and allow retry. A crash after step 4 resumes the committed next state. PCM files remain per commission; the artifact sourceHash identifies the input material, not a checksum of output PCM. No cross-file all-or-nothing transaction is claimed; payload-first/manifest-last ordering preserves the earlier checkpoint on failure.

SaveStableProgress and photography save functions return success/failure. Recording and photo save errors block global checkpoint advancement and park departure. Existing exit callers that do not unload scenes may keep progress in memory and retry later. Forced application termination cannot be prevented.

PCM writes serialize through a separate writer lock. The audio lock is held only while taking a consistent snapshot; disk writes and flushes occur outside it. File IO remains synchronous on the caller; background IO and performance tuning are deferred.

## Deferred verification

- Compile all changed scripts and import new assets/bindings.
- Load existing v1 story and tape v1–7 fixtures, then save/reload new records.
- Reorder work tape bindings and confirm v8 recovery follows IDs.
- Duplicate-ID and unknown-ID rejection.
- Inject PCM/story/photo write failures; confirm no premature success or scene departure.
- Submit all three commissions and restart after each submission boundary.
- Confirm authored transforms and anchors remain unchanged in Play.

The offline authoring script is Tools/Migrations/author_data_bindings.py. It writes bindings only once per scene and never edits Transform blocks. Future content should be authored directly in Inspector; do not rerun it to update models.
