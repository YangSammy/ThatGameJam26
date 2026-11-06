# Player completion shortcuts implementation plan

**Goal:** Enable 1–5 in normal and development Windows players at the user's specified completion nodes.
**Architecture:** Reuse the existing completion/commit paths. House and Park share key recognition and a one-shot cross-scene request; completion entry preserves the active save rather than switching to the development save.
**Tech stack:** Unity 2022.3, C#, existing Built-in project and Editor validation tools.

## Contract

1: finished tape commission popup. 2: full park roll and return-to-house panel (no darkroom completion). 3: finished/dried prints ready for viewing and continuation. 4: existing autumn farewell/completion. 5: existing winter ward completion and continuation. Main keyboard and numpad; key-down only; block during load/pending photo save; repeat completion does not rewind chapters. Enable ordinary builds without requiring Development Build.

## Tasks

- [ ] Establish failing player compilation/symbol coverage and active-save entry regression.
- [ ] Expose completion dependencies to players; preserve legacy Shift shortcuts in Editor/development.
- [ ] Route House 2 to completed Park, Park 1/3/4/5 to House completion; retain save/error gates.
- [ ] Validate stage outcomes and repeat/busy behavior in Editor, compile ordinary/development player scripts and build the ordinary player when available.
- [ ] Record evidence and limitations; leave user scene edits intact.

Review focuses: normal save survives restart, both entry scenes, no duplicate running completions, photograph encoding remains protected, all helper methods and scene handoffs compile without UNITY_EDITOR/DEVELOPMENT_BUILD.
