# Late-route sunlight angle

User requests directing the sun so the latter park route feels sunnier and more exposed. Scope: rotate Light_Sun, synchronize sky direction, preserve intensity/color, post effects, shade-aware rim and route layout.

- Preserve current dirty scene with SaveScene(saveAsCopy) before changes; user manually rotated sun to approximately (7.16,205.07,30.26).
- Route: bridge photo (5,.55,0), exit path then meadow photo (14,.09,8). Photo view yaw90 and61.7 respectively.
- Sample candidate sun visibility and inspect native renders at bridge, exit, meadow and raised-camera meadow views.
- Choose angle from visual evidence; persist only final sun transform over the pre-task live scene snapshot and save synchronized sky material direction. Keep other user edits.
- Verify reload/persistence, sky alignment, late-route visibility, camera/subject restoration, existing sun checks and Console.
