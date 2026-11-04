# Camera preview implementation

User approved realtime exposure/DOF preview, aperture-dependent clarity and better focus wheel on 2026-11-04. Implement in the existing checkout; no scene/layout/anchor/save-format changes. Preserve other work. Use Unity feature-implementation and verification workflows, no extra approval or commit.

1. Back up relevant scripts/shader, scene hashes and original saves; inspect Editor/Console baseline.
2. Add one shared camera-response calculation: exposure error/gain, aperture-dependent clear band and nonlinear focus stepping. Keep f/8 clear band compatible, widen toward f/16, narrow toward f/2.8 with a forgiving minimum. Preserve focusStep as nominal speed at 4 m. Test sign/equivalence, near/far stepping, clamps and aperture ordering.
3. Extend ParkPhotoFocus/ManualFocus in place. Same shader used for viewfinder and actual Camera.Render capture. Apply exposure in linear color; neutral exposure must be identity, overexposure has a soft shoulder. Bound blur and make radius relative to image height. Avoid changing natural exploration/reference images.
4. Integrate authoritative settings before rendering/capture, use shared clarity in task readiness/hints/record assessment; apply only the active viewfinder wheel. Existing loading, fixed FOV, persistence and entry/exit unchanged.
5. Unity compile, deterministic checks and real GPU probes. Verify neutral/under/over/equivalent exposures, DOF ordering, preview-vs-saved photo, actual wheel/mode transitions and setting recovery. Label test fixtures separately from native input.
6. Stop Play, restore original saves byte-for-byte; confirm scene files unchanged, document evidence and limitations.

Review focus: exposure sign; gamma/linear conversion; current-frame parameter freshness; reference capture accidentally exposed; pause or instrument mode lingering effects; f/8 compatibility; no historical-photo rewrites. No shutter motion blur, grain, dynamic metering or optical FOV recalibration this iteration.

Completed 2026-11-04: runtime, native wheel/shutter/exit checks, GPU parity and save restoration finished. Evidence and limitations: [validation report](../../validation/ParkCameraPreview-2026-11-04.md).
