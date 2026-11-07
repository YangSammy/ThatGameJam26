# Winter 2D motion fixes — 2026-11-07

Crate collision now resolves to the actor's pre-step side, and pushes only when grounded and moving toward the crate. Collision clears horizontal residual velocity. Swing/flower boarding requires non-positive vertical velocity and either a ground approach or a position above the seat (0.1-unit tolerance). Fixed jump velocity remains unchanged.

Before: stationary actor on crate right at x=2.61 jumped to x=1.38; upward airborne actors near swing/flower were snapped into riding state and velocity zeroed. After: 25 assertions passed covering both crate sides and three input directions, push direction, ascending rejection, descending boarding, below-seat rejection, and fixed-speed jumps from all seven ride positions. Unity script refresh succeeded and Console showed zero errors. Regression body: WinterMotionRegression.cs.txt (Unity MCP execute_code).

No mouse/manual playthrough or Build regeneration was performed.
