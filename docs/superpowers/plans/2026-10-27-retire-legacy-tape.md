# Retire legacy tape dependency

## Scope
Keep authored HouseScene/ParkScene transforms, camera anchors, radio animation and current gameplay. Replace the old tape station dependency with explicit player camera/hands references. Remove Whitebox and retired tape controllers/routes after migrating references. Preserve TapeChapter's current inspection model.

## Implementation
1. Capture current scene anchors, radio transform and existing player hand references; save existing editor changes without rebuilding scene content.
2. Make WBMain use its player camera, FirstPersonHands and HouseRadioDeck directly. Remove the standalone whitebox control loop and fallback route.
3. Register the radio hand set from the existing camera-owned hand visuals, without old device hand targets. Keep other chapter targets and park hand poses.
4. Update inspection authoring and radio tools to use the current scene controller. Remove obsolete inspection upgrade code.
5. Remove old scene objects, standalone Whitebox scene, retired scripts and obsolete test harnesses; keep shared resources that are still referenced.
6. Replace obsolete regressions with current-runtime checks: startup without legacy object, all chapter entry/exit and pose restoration, radio pick/scan/drag-load/playback/re-entry, source-safe inspection, and park film roundtrip.
7. Verify compile, missing references, anchor invariance, runtime behavior and the current two-scene build configuration. Keep validation output separate from production data. Stop Play and restore HouseScene afterward.

## Completion evidence
Record actual pass/fail results and any validation limitations. No automatic commit or push.
