# Fifth-day departure implementation plan

**Goal:** Complete the user's fifth-day specification and replace the permanent white screen with a brief soft fade, title, and return-to-menu action (user selected).
**Architecture:** Keep EndingChapter/OpenDoor/CrossThreshold and unified saves authoritative. Add a small WBMain presentation partial; extend the existing DepartureBedroom presentation without new progression requirements.
**Tech stack:** Unity 2022.3, built-in rendering, existing IMGUI and scene routing.

## Approved design

Return from winter to the same bedroom. Snow recedes and daisies open without clicks. Existing three photos, tape storage, crumpled letters and final drawing remain observable. Door seam, floor light and distant air guide the player. Opening does not complete the story; crossing does. No flower collection, necklace, cemetery or task text. After crossing, hold the scene briefly, fade into warm light over about two seconds, then show the product title and one Main Menu button. No forced walk.

## Tasks

- [x] Reproduce permanent-white completion and record missing end-card behavior.
- [x] Replace white overlay with timed ending presentation and safe menu navigation; preserve completion/save ownership and pause/focus behavior.
- [x] Add meltwater and crossfade winter wind into existing spring window audio, plus distant door air. Preserve authored transforms and existing keepsakes.
- [x] Validate no extra gates, opening vs crossing, presentation timing, keepsakes and menu return; compile and rebuild ordinary player.

## Validation focus

Completed-save reload, input held during crossing, pause/focus loss, no early completion outside doorway width, no duplicate audio on reload, no scene placement edits lost. End card must be absent before crossing and usable after fade. Player build must include the same completion flow.
