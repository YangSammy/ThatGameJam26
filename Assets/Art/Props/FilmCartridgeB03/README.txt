B03 35 mm film cartridge: standalone ISO100 / ISO200 / ISO400 / ISO800 prefabs.
Unity 2022.3.62f3 Built-in Standard. No gameplay, input, save or scene integration.
Root aligns B01 Visual/InsertAnchor at (-.048,.034,-.006), zero relative rotation/unit scale.
Shell, CartridgeSpool, Outlet and FilmLeader are separate. +Y up, +X transport, +Z lensward.
Rigid part pivots are normalized. Film retains original positive FBX renderer orientation to keep morphs in compact binary storage.
10 clips, 177 film morph targets; no Animator. Never mix unrelated morph states manually.
Use coupled clips for pull/seat/advance/rewind; stable-state clips restore all weights and outputs.
In later approved integration, B01 take-up follows TakeUpDrive and rewind knob follows CartridgeSpool.
Insertion/removal move CartridgeAssembly only; first open back, lift rewind, use ready/retracted leader state.
All hit suggestions and separate physics proxy disabled.
Textures: BaseColor sRGB; normals NormalMap; MetallicSmoothness linear R=metallic,A=1-roughness.
Editable Blender, source PBR layers, actual Unity previews, drive table and checks:
output/B03_FilmCartridge_v1/ at project root. Preserve Bfont-License.txt.
