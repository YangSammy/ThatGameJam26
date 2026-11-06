# Player completion shortcuts — 2026-11-06

## Behavior

Both ordinary and Development Windows builds accept the main keyboard or numpad 1–5 during gameplay (without Shift).

| Key | Destination |
| --- | --- |
| 1 | Tape commissions submitted; normal completion presentation |
| 2 | Park roll filled; normal return-to-house panel |
| 3 | Darkroom prints finish drying; hanging print can be opened to continue |
| 4 | Autumn farewell and normal chapter completion |
| 5 | Winter ward's normal concluding walk and completion |

House and Park both handle these shortcuts. Park routes 1/3/4/5 through House with a one-shot pending request. Main menu does not handle them. Loading/brush transitions and pending photo saves reject input; retry when ready. Already completed chapters are retained rather than rewound. Shortcuts use and update the current save, including ordinary player saves. Existing Shift debug entries remain Editor/development-only.

## Changes

- Removed Editor/development compile guards from completion paths and their required helpers.
- Added shared row/numpad key recognition and cross-scene pending intent.
- Added `FiveDayProgress.PrepareShortcutEntry` to skip prerequisites while retaining the active save profile.
- Key 2 now ends in Park rather than entering darkroom.
- Review found navigation could be silently rejected during Brush reveal. House completion now waits for it; House/Park reject new requests during active transitions. Pending House requests do not block the normal transition tick.

## Evidence

- Before changes, ordinary player script assembly omitted `WBMain.EditorCompleteStage` (expected regression failure).
- Final ordinary and Development player script compilation: PASS; 11 entry/helper methods present in each assembly.
- Ordinary Windows build: **Succeeded**, 0 errors, 4 warnings, 4,001,213,094 bytes. Output: `Build/PlayerShortcuts/MemoryGame.exe`.
- Direct inspection of the built player's `Assembly-CSharp.dll`: final House `IsBusy`/`ExternalLoadActive` guards and Park completion entry present.
- Warnings: uncompiled editor/import changes during build, one existing Screen Space Reflection shader warning, two MCP websocket warnings. The built runtime assembly was inspected separately to confirm the final shortcut guards were included.
- Read-only review completed; its transition timing finding was addressed.
- Play Mode was repeatedly interrupted by concurrent script recompile/domain reload. **The complete 1→2→3→4→5 runtime sequence and physical key input in the executable are not yet verified.** Compilation/build success is not an end-to-end gameplay result.
- Original EditorPlaySession restored; Summer scene reopened. Scene item placement was not edited by this task.

Raw compilation/build evidence and backed-up runtime test data: `output/QA/PlayerShortcuts-20261106/`.
