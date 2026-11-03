# ParkGirl independent technical sample

Open `Scenes/ParkGirlSamplePreview.unity` and enter Play to inspect the 10-second animation. `Prefabs/PF_ParkGirl.prefab` is independent and has not replaced any production scene character.

Current version: second refinement pass. Hair and jacket geometry have been rebuilt. The 9.27 mm neck error was traced to missing left/right shape-key masks and fixed; all 301 sampled frames now remain below 0.073 mm. Hair covers the upper face, while a complete featureless head and independent skin-colored guard maintain anonymity. The detailed Rain head is retained only in the editable source archive and is not exported.

This remains an art sample, not a production-integrated character. New hair/fabric normals are procedural directional detail, while AO is neutral. The high bone count is not yet optimized. See the current report `output/Characters/ParkGirl/Reports/RefinementV2.md` and actual Unity images in `Preview/Unity/V2`.

Editable Blender source, procedural texture parameters, license notices and detailed evidence are in the project folder `output/Characters/ParkGirl/README.md` and `Reports/Validation.md`.

Rain Rig (CC) Blender Foundation | studio.blender.org — CC BY 4.0. Derived and modified for this sample. See `output/Characters/ParkGirl/THIRD_PARTY_NOTICES.md` for full attribution, links and embedded tool licensing.

Editor menu tools save only this sample's assets. Play verification is explicit; full multi-scene workspace restoration has not been implemented. Use the single-scene preview workflow.
