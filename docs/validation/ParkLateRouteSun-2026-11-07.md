# Park late-route sunlight

- Saved Light_Sun rotation: (22, 250, 0) degrees; intensity remains 1.28. Sky direction synchronized to (0.871268, 0.37460664, 0.31711563).
- Compared native HDR camera renders at bridge, bridge exit, meadow and meadow viewfinder. Selected 22-degree elevation for brighter bridge decking, a larger sunlit meadow approach and warm character highlights. Existing grading, bloom and sun/shade rim settings remain unchanged.
- Preserved the user's initially dirty scene in Backups/ParkLateRouteSun-2026-11-07/ParkScene.live-before.unity. Final scene differs from that snapshot only in the sun Transform rotation and Euler hint. Sky material differs from its backup only in sun direction.
- Reload verified saved rotation, intensity and sky alignment (dot=1). Camera and companion restored to original positions.
- Existing checks: solar 11/11, sun/shade 10/10. Console contains an unused-field warning for ParkPhotoPrototype.dwell; no errors returned.
- Final native images: output/ParkLateRouteSun-2026-11-07/final-{bridge,exit-path,meadow,meadow-viewfinder}.png. Edit-mode captures validate lighting/post-processing; they do not reproduce the complete gameplay camera-raising animation.
- No executable rebuilt. Rebuild required for the existing standalone player to receive this scene change.
