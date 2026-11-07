# Tape return navigation and right-button inventory — 2026-11-07

## Changes

- The tape chapter creates a bottom-centre native UI tab. Hovering its fixed zone slides up a Return to bedroom button. Click uses WBMain.ReturnFromTapeButton -> PlayerInputContext.RequestExit -> ExitTapeDeskView, closing preparation/recorder modes before the saved house exit. Pause, transition, inspection, help and success modal states hide it. Its screen region blocks world picking and drop targets.
- Workbench cassette rotation is right-button press/drag/release. It no longer requires the left button. Rotation commits on right release, and the workbench no longer treats a right click as navigation. Right press still cancels another active drag, including original A cassette movement. Archived/locked/loaded tapes retain their guards.
- Independent E inspection keeps its existing controls; the change concerns physical workbench cassettes.

## Remaining right-button back/cancel actions

| Context | Current behavior | Owner |
| --- | --- | --- |
| Tape recorder view | Unfocus radio/preparation close-up first, otherwise exit station | HouseRadioDeck.Recording.cs / ExitTapeWorkspace |
| General station (including tape outside its workspace modes) | Cancel current gesture first, otherwise saved station exit | WBMain.Input.cs |
| Object inspection | Cancel gesture first, otherwise close; E also closes | SafeInspection.cs |
| Photo gallery | Close gallery | WBMain.Input.cs |
| Direct darkroom | Cancel interaction and save; no level exit when idle | WBMain.Input.cs |
| Park camera/meter | Cancel drag first; aperture/shutter -> viewfinder; other instrument modes -> exploration | ParkPhotoPrototype.Input.cs |
| Autumn writing | Cancel writing/paper drag first; otherwise saved station exit | WBMain.Input.cs / WritingWorld3D.cs |
| Winter album/ward | Cancel focus/key/door interaction first; close an open book before phase 2; otherwise saved station exit | WBMain.Input.cs / PaintingWorld3D.FirstGap.cs |
| House help | Close help | WBMain.Input.cs |
| Held tape in room exploration | Put tape back | WBMain.Input.cs |
| Tape workbench | Rotate a cassette or cancel another drag; no navigation | HouseRadioDeck.Workbench.cs |

The shared back click triggers on RMB release with pointer displacement <=8 pixels; input readiness, pause and transition gates still apply. In independent inspection, holding LMB+RMB claims rotation rather than back. Park completed-film handoff blocks right-back; Park exploration has no back destination. Camera look, pointer release, and physical inspection rotation are not level-return actions.

## Validation

- Pre-change isolated WorkbenchPointer test failed: right-only down did not start rotation. Final test passed right-only down/drag/up and original-A drag cancellation.
- Native return UI fixture passed: collapsed initial position, hover reveal/clickability, callback reaches saved-exit command, missing/failed save prevents departure, and UI hides outside the tape chapter. Fixture hosts intentionally cannot save; production saves were not touched.
- Unity Editor refreshed/compiled; no compile errors. Scoped git diff --check passed.
- Independent review found original-A event ownership swallowed cancellation; the cancellation now precedes SourcePointer and passed its regression check.

No full Play Mode traversal or rendered hover animation acceptance was performed. Existing uncommitted work was preserved. Pre-edit snapshots: output/validation/tape-return-20261107/before.
