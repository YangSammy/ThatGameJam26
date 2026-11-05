# Camera body / eyepiece entry and hover

- E from Exploration now always enters Body, regardless of loaded film. E from instrument/finder lowers the camera. Only clicking the ready eyepiece starts the existing camera-raise transition to Viewfinder.
- Existing hover already covered rewind knob, selectable cartridges, leader, advance lever, back, aperture and shutter dial. EyepieceAnchor had no Renderer and HoverPart lacked a mapping, so it could be clicked without an outline.
- Eyepiece and film chamber now use disabled, collider-free local outline proxies because their geometry is combined into Body_Mesh. This is a rectangular local interaction outline, not a new visible model or a whole-camera outline.
- During cartridge/leader dragging, the hover ray uses the same receiver as release and ignores the held visual; only the valid chamber/spool is highlighted.
- Scene layout, source Prefabs, rig, saved film and progression unchanged. No save/Play session touched this turn.

Validation: initial unrelated winter compilation errors cleared during this task. Latest compiled code passed Edit-mode checks: loaded E opens Body, next E lowers; eyepiece/chamber produce disabled outline-only geometry without colliders. Final Console query has no errors. Static diff check passed. No Play screenshot/native mouse acceptance claimed. Backups: output/QA/ParkEyepiece_20261105/before.

Manual checks after compilation is clear:
1. Unloaded: E opens Body, eyepiece does not outline/activate. Repeated E only alternates Body/Exploration.
2. Loaded: E still opens Body, hover eyepiece shows local white border, LMB starts raise transition; only its end enters finder.
3. E/Esc during raise cancels safely, loading remains ready. Reopen with E, click eyepiece again.
4. Hover each available rewind knob, cartridge, leader, advance lever, back, aperture and shutter dial: only that part outlines, unavailable parts do not.
5. Drag cartridge toward chamber / leader toward spool: receiver white border appears only over valid target. Release elsewhere does not complete the step.

Performance suggestions (not implemented in this task): entrance uses a small anticipatory weight shift and brief head/hair motion; bridge turns head then shoulders/chest then hair settles, followed by a sustained waiting pose; meadow has a readable reach/offer, gentle grip closure and two-arm contact. Record actual pose at shutter time, never snap the pose just for the saved frame. Preserve readiness after response, don't make players race a short pose window. Free-roam face concealment must be checked at every reachable angle.
