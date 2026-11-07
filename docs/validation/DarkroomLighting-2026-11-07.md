# Darkroom lighting and fixed exposure

Changed Bedroom_Summer only: existing Safelight now red (1, .12, .055), intensity 2.1, range 4.2, soft shadows at .7. Added a local warm worktop bounce (intensity .65, range 2.65), no shadows. Exposure Indicator, prop transforms, colliders and workflow are unchanged.

SummerPhotoRoom clones the existing color response material at entry. Its darkroomExposure is .55; crossing the curtain blends spatially over .8m from the original 1.15. Looking at dark objects does not alter exposure. The shared material is unchanged and the instance is released on scene destruction. This uses the existing fixed tonemapper, not Beautify or a second postprocessing pass.

Validation: Editor compilation succeeded; Console error query empty. Live method checks returned exposure 1.15 / .85 / .55 / 1.15 at outside / transition / inside / outside. Shared material remained 1.15. Reviewed 1280x720 offscreen entry-camera render with the same exposure material. Screenshot: output/QA/DarkroomLighting-20261107/after.png. These are Editor checks, not a Play-mode walkthrough or performance measurement.

Original scene backup: Backups/DarkroomLighting-2026-11-07/Bedroom_Summer.unity. ParkScene had unsaved changes before this task; these were preserved. Summer scene was loaded additively and closed after saving.
