# Autumn farewell sequence — 2026-11-07

Implemented approved idle -> stationary wave -> turn -> walk out -> EasyTransition winter sequence.

- WritingChapter owns 2.3s wave, .65s turn and 2s walk. Completion waits for all stages. Farewell restore returns safely to arrival.
- Existing IdleLoop, wave source segment 2.35–4.65s and WalkInPlace clips are reused. Prefab and authoring bind walk.
- World placement reads chapter time, not a second accumulated timer; girl stays put during wave/turn. Doorway exit hides the girl at the edge of the miniature set.
- Committed autumn completion starts existing ChangeBedroom directly. CompleteHouseExit runs only after Brush cover; existing bedroom loader handles winter and reveal.
- Before change, 2.1 seconds already completed the chapter (confirmed).
- DepartureSequence, FullRowFarewell and FarewellContract passed at 30/60/120Hz. Actual rig sampling showed nonzero idle hand, wave hand and walking foot motion.
- Rendered production memory world at four timeline positions; adjusted exit path to avoid unbuilt exterior. Compilation and scoped diff whitespace checks passed.
- No standalone build, production save modification or full native-input scene-transition playthrough performed. Running Player remains its previous build.
