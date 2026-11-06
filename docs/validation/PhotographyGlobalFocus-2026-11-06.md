# Whole-image manual focus

User-approved simplification: the 50 mm lens still changes focus distance, not focal length. The entire photographic image now shares a blur amount derived from the same subject depth and clear range used by shot acceptance. Aperture retains its existing effect on that clear range. Outside the clear band, a smooth curve increases uniform Gaussian blur. No scene-depth masking or central split-image patch remains.

Runtime changes:
- `ParkPhotoFocus.cs`: shared FocusBlur response and subject-depth input; uniform blur radius up to 28 pixels at 1080p.
- `ParkPhotoPrototype.cs`: feed task acceptance depth into the preview. Existing reference points remain authoritative; no change to acceptance, film data or save format.
- `ManualFocus.shader`: two-pass whole-image blur, existing exposure and camera-raise fade preserved.

Capture continues through the existing RenderPhotographicFrame path, so new exposures store the effect before PNG encoding. Dial background blur remains preview-only. Existing saved photos are not regenerated. IMGUI text/cursor and instrument detail overlays are drawn outside this effect.

Validation: Unity compilation and isolated ParkFocusRenderingChecks.Run passed, including two geometry depths becoming sharp together, defocus softening, exposure capture matching preview blur, obsolete focus-aid flag having no effect, lowering/restoring the camera, dial backdrop separation, unchanged aperture tolerance and exposure GPU checks. Focused edge contrast 0.7255 versus defocused 0.0588 in the synthetic test. Console error query returned zero entries; Console was not cleared.

Evidence: output/QA/FocusRendering_20261106/checks.txt and its PNGs. These are isolated rendering checks, not real-mouse gameplay or a new darkroom end-to-end acceptance run.

Manual check: scroll through near/correct/far focus at each photo point; whole scene should soften/clear together while labels stay sharp. Visit each dial and return; its backdrop blur must not stick. New developed photos should reproduce the shutter result; older photos should remain unchanged.
