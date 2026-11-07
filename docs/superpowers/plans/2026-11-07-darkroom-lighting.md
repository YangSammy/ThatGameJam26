# Darkroom basic lighting and exposure

Goal: implement the requested first lighting pass: red safelight, readable worktop, fixed exposure.
Architecture: retain existing BedroomColorResponse; create a runtime material instance for SummerPhotoRoom and blend exposure by camera position across the annex doorway. Keep the shared bedroom material unchanged.

- Back up Summer scene and inspect entry camera preview.
- Tune existing safelight and add a low intensity local bounce light.
- Add serialized darkroom exposure; restore bedroom exposure outside the annex and destroy the runtime material on unload.
- Check entry/exit exposure, material isolation, Console, saved scene diff and offscreen preview.

Scope: no Bloom, no gameplay layout or processing timing changes, no Play-mode save changes. Preserve unsaved ParkScene.
