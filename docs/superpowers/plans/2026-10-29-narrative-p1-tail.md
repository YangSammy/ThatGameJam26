# Narrative P1 tail implementation plan

Goal: replace legacy autumn/winter/ending rules with v3 while retaining the existing HouseScene assets and view anchors.
Architecture: Chapter owns versioned rule/checkpoint state; WorldChapter3D owns presentation and pointer adaptation. Shared PlayerInputContext owns input release and exit. Configuration uses ChapterDefinition assets. New narrative scene bindings are additive.
Spec: v3 complete gameplay PDF pages 8–14, auxiliary PP-05–08. Later user decisions keep concise English assistance and continuous house operations. No unrelated asset replacement.
Execution: inline, implement all modules before compilation/testing as explicitly requested. Then compile, run rule regressions, inspect House/Park and exercise available runtime paths. No passing claim from source inspection.

- [x] Autumn: tutorial, crumple, formal timed four-object collection, independent clock, simultaneous writing/E, arrival latch, farewell; v2 checkpoint and old-save mapping.
- [x] Winter: fixed observation, discoverable shapes, book raise/lower; 3D side route, swing and bridge; suitcase pressure gate, upright flower carriers, high checkpoint, unique manual pendant insertion.
- [x] Tail: persistent physical paper through white transition, monotonic distance-driven seated ward reveal, nose-marker cut, no third puzzle.
- [x] Spring: existing room door interaction and crossing, snow/flower/light changes, preserve earlier chapter artifacts and authored transforms.
- [x] Shared integration: input contexts/release, chapter configuration/scene references, safe cancellation and checkpoints; no save of held controls or vehicle parenting.
- [ ] Final regression: compilation first, isolated rules, runtime scene smoke and available full-flow checks, save failure/recovery. Report untested manual animation/visibility paths explicitly.

Review focus: timeout boundary vs arrival; pause without time exploit; missing receivers/anchors; held inputs across book modes; key ownership on failed/cancelled drag; body crossing threshold without needing a key item; restoring mid-transition without revealing a full face.

Validation evidence: QA/P1-2026-10-29. Ten narrative rule cases, 25 scene integration assertions, five priority regressions and tape sample transport regression passed in an isolated complete project copy on Unity 2022.3.62f3. House/Park/House lifecycle has zero runtime errors. Existing House transform values unchanged; two new anchors added. Full darkroom drag sequence remains failing at exposed-film -> bag; do not certify the whole project as passing. Old HouseCursor/HouseGameplay/PhotoRecovery suites assume retired fixed-view workflows and are not evidence for the current continuous-space flow. Windows build and manual visual inspection are tracked separately.
