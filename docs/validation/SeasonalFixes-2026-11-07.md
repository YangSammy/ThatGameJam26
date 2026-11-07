# Seasonal fixes — 2026-11-07

- Credits: removed the 40-second blank hold (start 42.5 -> 2.5 seconds), find inactive authored UI, activate on ending, force background below content, use the host font. Preview scene verified seven rows and 520 pixels of travel in ten seconds. No claim of live rendered verification.
- Autumn/winter photos: load developed roll only, map frame 1 to Photo_original_print_00 etc., including duplicate observation-room surfaces. Preserve frame geometry; normalize mesh UV range and stretch the image over it. Current local roll bound 3 autumn and 6 winter surfaces. Remaining authored frames without a matching exposure remain unchanged.
- Act-two missing shapes: extend dashed outline proxy machinery to luggage and flower; preserve act-one alpha hints.
- Winter flower: confirmed Park/V10/Meshes/DaisyCluster_LowPoly placeholder with 1.03 x 0.45 x 1.47 m bounds. Replace at runtime with existing writing bouquet, fitted to 0.45 m height; preserve clue binding and resize hitbox. Preview validated replacement and old renderer disabled.
- Writing: remove generated satchel and show only collected luggage. Girl faces player during wave and then turns toward exit using existing departure timing.
- Winter final run: D holds and rising edges advance cursor; D icon and progress bar. Reveal progress uses runProgress^2.5 with shader ease 1; smoke clock follows progress instead of wall time. Final top remains calibrated UV 0.688. Model tests: hold 20.7 seconds, 6 Hz taps 13.57 seconds; halfway reveal 0.1768, endpoint 1.0. No-input progress remains stationary by code.

Unity script compilation passed with zero errors before scene probes. No mouse interaction, player-save writes, or Build regeneration. Visual framing and final aesthetic timing have not been manually viewed.
