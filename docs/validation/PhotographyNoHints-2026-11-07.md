# Photography text guidance removal — 2026-11-07

User scope: remove text hints in Park photography and House darkroom, including meter tooltips; retain the physical meter screen UI.

Removed Park HUD instructions/notices/film counter, keyboard shortcut captions, shutter-ready wording, duplicate meter HUD text, darkroom step/progress text, house photography keyboard/help/hover text, and photography tooltip bubbles. Hover outlines, visual markers, camera numeric readouts, physical meter screen labels, print pagination and actionable button labels remain. The photography H-help shortcut no longer opens a hidden blocking page. Gameplay/save state and meter calculation/rendering code are unchanged.

Validation: Unity Editor refreshed and compiled successfully; final Console error query returned zero entries. Scoped git diff --check passed. SHA-256 confirmed PhotographyMeterDisplay.cs unchanged. An isolated preview-scene fixture instantiated the actual configured meter prefab: Apply(400,13,true,1,8) retained enabled MeterSettings = ISO 400 / f/8 and MeterShutter = 1/500 s; unavailable readings retained --. Independent static review found a remaining Close / Esc caption, now reduced to Close.

No Play Mode traversal or full visual acceptance was performed. Production saves were not touched. Scoped snapshots: output/validation/photography-no-hints-20261107/before.
