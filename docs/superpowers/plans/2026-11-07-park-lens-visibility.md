# Park Inspector lens controls

User-approved scope: remove the golden-profile hard disable of Amplify dirt,
flare and glare; preserve natural walking colors and bridge/meadow golden optics.

Implementation:
- Keep existing serialized strength fields and the user's Inspector values.
- Strength = Inspector maximum × camera-raise blend × sun viewport weight.
- Multiply only these three lens contributions by the existing smoothed 1×1
  pre-bloom HDR solar visibility texture in Amplify's final composition.
- Reuse solar visibility outside the two golden photo sites when lens effects
  are requested. Golden site eligibility and golden intensity remain unchanged.
- Walking Lens Blend = 0 keeps these three effects off with the camera lowered.
- Capture, raise obstruction and detail backdrop suppress the lens effects.
- No GPU readback, extra full-resolution render or global shader state.
- Other Amplify cameras use a white visibility texture by default.
- Starburst remains independently controlled by the existing Amplify setting.

The project-specific Amplify extension is in AmplifyBloom.cs and BloomFinal.shader;
preserve it when updating the third-party package. Solar visibility remains an
HDR brightness estimate at the sun disc, not geometry-based shadow detection.
The existing Golden Backlight material supplies its sampling shader.

Validation scope: inspect changes and Unity compilation/Console only; no Play
Mode, image comparison, automated test suite or build (following the prior
implementation-only instruction). Do not save unrelated pending scene edits.
