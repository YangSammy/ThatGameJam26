# Tape workbench layout and door feedback

## Changes

- Bedroom_Spring: storage moved forward beside the left side of the preparation mat, by world delta (0.429, 0, 0.347) m. Existing eight cassette identities, original home anchors, finished copies and storage close-view anchor moved with it. Other operation cameras stay unchanged.
- Dispenser rotated 90 degrees about world up and positioned across the rear of the mat. The complete visual and its grab/cutter children move together, preserving mechanical animation axes.
- Three original-tape desk landings moved to the gap between box and mat; work-tape landings unchanged. Box membership now uses the assigned liner footprint, avoiding classifying nearby desk tapes as still inside the box.
- Both door hitboxes and loading receivers fitted to the actual door face. Existing collider identities and commands retained. Their bounds no longer extend over the key row.
- Authored door frame/window Renderer references drive both hover and valid drop outlines. Old blue rectangular slot guides remain disabled. The preparation-mat preview is unchanged.
- A physical transport key under the cursor rejects cassette loading. Ordinary click picking uses the same small-part resolver as workbench interaction.
- Preparation waiting-for-release blocks hover only while the preparation context is active. Previously that stale flag hid recorder hover even when its buttons accepted clicks.
- Returning a cassette no longer re-enables retired whitebox renderers/colliders for one frame. This fixed a reproduced false desk-obstruction failure during ejection.

No recording rules, chapter completion, audio, save format or item IDs changed.

## Files

- Assets/Scenes/Bedroom/V17/Bedroom_Spring.unity: positions, hitbox sizes, door surface bindings, liner binding, three landing anchors.
- Assets/Scripts/Chapters/Tape/DualDeckVisual.cs: serialized door outline surfaces.
- HouseRadioDeck.Hover.cs: door hover and context-specific release gate.
- HouseRadioDeck.DropPreview.cs: white door silhouette, mechanical-key drop rejection, legacy guides disabled.
- HouseRadioDeck.cs: consistent pick resolver and cleanup on disable.
- HouseRadioDeck.DualDeck.cs: retain disabled legacy visuals during visibility changes.
- TapeWorkbenchVisuals.cs: assigned box interior footprint for membership.

## Validation

Unity Editor Play Mode, using MCP to call the normal ray picker and pointer event handlers. This is runtime interaction-path validation, not native physical mouse testing or a full three-commission regression.

- Both authored camera viewpoints: 14 control-center rays passed; expanded to 42 horizontal center/edge samples, all hit their intended five live keys and two doors, including an open A door.
- A door hover selected exactly its six authored surfaces. Actual game-camera render saved.
- Empty B door accepted a drop probe; B Record key rejected it. Both legacy colored LineRenderers remained disabled.
- Cancelling the drop cleared its outline selection.
- Box membership: all 3 originals and 5 work tapes inside; all 8 desk landings outside.
- Calling the native recorder pointer-down handler on A door started only A ejection. After animation, A unloaded at the desk landing with no obstruction notice.
- MouseDown/MouseDrag/MouseUp through SourcePointer moved that tape from the desk into A; loading completed with door closed. B stayed idle.
- No error/warning entries on final Console query. No Console clearing used.

Initial ejection failed because legacy renderers were briefly re-enabled. Fixed and retested; not treated as a passing initial run.

## Evidence and recovery

`output/QA/TapeLayoutHover_20261105/`: original scene copy, git baseline, initial and updated layout renders, Play Mode hover/drop renders, ray results and ejection results. Scene backup includes pre-existing uncommitted work. Do not reset the whole git branch to undo this change.

`EditorPlaySession.before` preserves the pre-test save. Test session archived separately, then the original directory restored with SHA256 verification. No scene or user save files deleted.
