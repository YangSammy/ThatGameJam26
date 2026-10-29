# Input contexts — 2026-10-29

`PlayerInputContext` owns permission and exit consumption. House and park each own one session; the house injects its session into SafeInspection. Existing gameplay controllers retain their gesture/animation states and key mappings. No scene/prefab/anchor edits are required.

| Context | Input owner | Esc |
| --- | --- | --- |
| House exploration | WBMain | Cancel active direct tape drag; otherwise unchanged |
| House station | CurrentWorld through WBMain | Cancel active photo gesture; otherwise leave station |
| Photo gallery | PhotoWorld3D | Close gallery |
| Tape workbench | HouseRadioDeck | Cancel drag/reel/rotation gesture; next press closes workbench |
| Tape transport | HouseRadioDeck | Stop transports, save and leave controls |
| Safe inspection | SafeInspection | Release current gesture; next press closes inspection |
| Inspection E / explicit Exit button | SafeInspection | Close inspection directly, restoring source state |
| Park body/instrument modes | ParkPhotoPrototype | Cancel held part first; otherwise return to exploration |
| Park exploration | ParkPhotoPrototype | Pause |
| Paused | Current player | Resume only after input release |
| Tape success popup | TapeTransportPanel | Click to dismiss, as before |
| Unfocused/transition | No gameplay owner | Gameplay blocked |

## Rules

- A context change arms a release barrier and suppresses mouse-look deltas.
- Keys pressed while a barrier is waiting must also be released. Relevant actions include pointer buttons, gameplay keys, Shift and M; wheel deltas are not queued or replayed.
- All gameplay/UI exit paths use `ExitPressed` or `RequestExit`. They consume the exit frame and arm the next release barrier before calling the controller.
- Context entry does not automatically clear domain state. The existing controller cancels its drag, stops its transport and saves its stable state.
- House direct source-tape/adhesive presses explicitly adopt the same initiating gesture; this is not a general bypass of modal or focus rules.
- Tape button callbacks, retry GUI, inspection GUI, house photo GUI and park pause UI check the current context. UI appearance alone does not authorize an action.
- Safe inspection restores cursor policy through the house callback, not an old snapshot of Cursor.visible/lockState.
- Park retains a locked OS cursor and its virtual pointer in instrument modes.
- Public low-level gameplay methods remain callable for authoring/automation; player-facing routes enforce context permission.

## Adding a new mode

Add an explicit context ID, choose cursor policy, then select it from the owning player/controller. Gate both Update and GUI/event callbacks. Cancel transient gestures on focus loss/disable and on leaving. Route Esc through the shared exit method; supply a cancel callback returning true only when it consumed an active gesture. Never use the same press to both cancel a gesture and leave its mode.

Validation deferred at user request. No compilation or Play pass is claimed.
