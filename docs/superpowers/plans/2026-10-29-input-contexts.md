# Input context implementation

Scope: unify input ownership, handoff, exit consumption and UI permissions for HouseScene, ParkScene, tape controls and safe inspection. Preserve scene objects, transforms, gameplay mappings and park virtual-pointer behavior.

Implementation uses PlayerInputContext per player/session and existing controllers as gesture owners. Existing release gates remain as adapters for controller-specific initialization. One exit press is consumed once per frame. Explicit pointer adoption is limited to the initiating press of direct original-tape/adhesive dragging.

Implemented:
- Common context selection, cursor policy, release barriers and exit routing.
- House input routing for station, gallery, pause, exploration, tape and inspection.
- Park mode ownership, drag cancellation, pause/focus lifecycle and UI gating.
- Tape panel/workbench GUI permission checks and direct-drag handoff.
- Inspection gesture cancellation and host-controlled cursor restoration.

User requested implementation without validation. No compilation, tests, Play or scene modifications were performed in this increment.

Deferred checks: held-key handoff; same-frame Esc and pointer release; original/adhesive direct drag; E inspection exit; two-stage Esc; popup click-through; park virtual-pointer edge transitions; focus loss during every drag/recording; UI while unfocused; returning to authored camera anchors.
