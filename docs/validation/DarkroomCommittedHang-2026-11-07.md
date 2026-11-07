# Committed hanging pose

User reports both paper and negative remain flat beside the clip after release. Normal offline clip sampling already produced downward meshes, so the exact live trigger was not reproduced. A simulated flat/missing hanging sample reproduced the reported geometry: negative height .000223m; paper .000351m despite correct upright root orientation and clip position.

Added explicit committed-state Hanging blend-shape application after animation sampling. Clears progressive Hang/Flatten/Unroll and conflicting wet/coil shapes while hung. Sets Hanging to 100; resets it to zero when taken down. Existing clip grip position, scale, motion, save state and processing rules retained.

Checks on cloned current scene binding using actual pose method: starting from flat meshes, negative extends .164973m downward from clip (top 1.391m); paper extends .224932m downward (top 1.191m). Releasing committed state restores flat geometry. Console errors empty. This verifies a defensive state fix, not reproduction of the exact reported runtime cause; no full Play walkthrough or rebuilt exe verified.
