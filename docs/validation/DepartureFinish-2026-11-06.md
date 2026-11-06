# Fifth-day departure finish

## Cause and change

`WBMain.DrawHouse` previously painted an opaque warm-white texture indefinitely whenever `StoryFinished` became true. `TickSpring` stopped movement, and the departure room immediately muted window audio. There was no end-card or menu-return path. Completion itself was correctly triggered by crossing the open doorway.

Replaced the permanent overlay with a 0.5-second hold, 2-second warm fade and title/menu card after 3 seconds. The title uses `Application.productName` (currently Remember seasons). The menu button becomes interactive after its short reveal, saves existing progress and uses BrushSceneTransition. No UI is added before crossing.

Existing spring scene content is retained: seven shrinking snow patches, daisy petals, door seam and floor light, three developed-frame renderers, completed tape storage, crumpled drafts and the final painting. Added shallow non-colliding meltwater under the snow and spatial audio layers: winter wind fades out with thaw, existing spring-window audio fades in, and quiet air comes from beyond the door. Window/air audio fades with the ending instead of cutting immediately. No new interaction or collection requirement; no cemetery.

## Validation

- Editor runtime: departure room loaded and ready, seven meltwater surfaces created, no ending card before completion.
- A ray through the real handle opens the door; story remains unfinished and walking through the doorway is enabled.
- Crossing outside the doorway width does not finish.
- Crossing inside the open doorway finishes and commits the story without inspecting/collecting props.
- Fade at midpoint is between zero and one; title/menu is unavailable during fade and available after the short sequence.
- Actual ReturnFromEnding route returned to MainMenu, Brush busy state cleared.
- Console: zero errors. Final ordinary Windows build: **Succeeded, 0 errors, 0 warnings**.
- Evidence: `output/QA/DepartureFinish/checks.txt`, `build.txt`, room/end-card captures. These are Editor interaction-path checks, not an executable mouse/keyboard walkthrough of all five days.
- Original EditorPlaySession restored; test completion data preserved separately. Existing scene assets were not resaved by this task.

## Remaining verification

Listen to final sound balance and recheck the updated executable's winter-to-spring sequence on the target machine. The last UI color-isolation adjustment was compiled/built after the end-card screenshot.
