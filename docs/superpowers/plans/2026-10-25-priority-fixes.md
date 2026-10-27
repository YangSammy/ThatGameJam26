# Priority bug fixes implementation plan

Scope: preserve authored scene/settings; repair help input cancellation, tank agitation and photographic movement. No package or gameplay-content changes.

- [x] Reproduce slow agitation, held-input cancellation and table-edge penetration with isolated Editor checks.
- [x] Separate explicit scene generation from Open/Validate/Build; confirm destructive generation and preserve existing scene files.
- [x] Cancel chapter interactions at help transitions; preserve physical mouse-edge state so held buttons do not restart accidentally.
- [x] Accumulate meaningful tank movement during one hold; reset on each new hold.
- [x] Apply the same table bounds with and without a held camera, allowing axis-separated sliding.
- [x] Re-run regression checks and all existing gameplay checks in Unity, inspect Console and verify scene/settings hashes.

Evidence: QA/priority-regression-checks.json and QA/gameplay-checks.json. The current folder is not a Git checkout. Physical keyboard/mouse full-game completion is not part of automated coverage.
