# Park handheld film leader: build animation format

User scope: park handheld camera loading in the existing Build player. Code and serialized-data investigation only; no mouse operation or live visual acceptance.

## Evidence

- Existing player: `Build/Remember seasons_Data/Managed/Assembly-CSharp.dll`, dated 2026-11-07 04:22. Cecil comparison found PhotographyCameraVisual and PropAnimationSampling methods identical to the current compiled editor assembly. This is not evidence of a missing sampling code update.
- `PropAnimationSampling.SamplePropAnimation` clones non-Legacy clips at runtime and sets `legacy=true` before `SampleAnimation`.
- Existing player `resources.assets`: all seven referenced B03 clips have `m_Legacy=false` and **0 m_FloatCurves**. Their source assets each have 183 bindings, including 177 blendshape channels. Non-Legacy player data uses compiled animation storage; changing the runtime flag does not preserve the source Legacy curve payload during build.
- Logical placement is independent: `EndBodyPointer` accepts the spool receiver and saves loading step 4 without requiring successful mesh deformation. This explains why successful mounting does not validate visual sampling.
- Independent editor controller probes produce pull progress 0, .25, .5, .75, 1 and moving mesh tips. Geometry probe found no camera-shell occlusion. These editor probes alone cannot validate the original player.

## Change

Set only `m_Legacy: 1` on the seven clips referenced by PhotographyVisualAssets: LeaderReady, PullLeader, SeatLeader, Advance, Advanced, Rewind, Retracted. Preserve curves, timing, references, gameplay and saves. Compile the intended sampling format before player stripping instead of relying on runtime conversion.

## Verification

- Before/after StandaloneWindows64 clip-only AssetBundle serialization: before PullLeader/LeaderReady had zero float curves; after **all seven clips retain 177 float curves and Legacy=true**. Artifacts: `output/validation/film-player-clips-before/` and `film-player-clips-after/`.
- Four source prefab tests (ISO100/200/400/800): pull at .65 changes blendshape weights by 100; restoring ready gives maximum error 0. Stages 0 through 5 sample without exceptions.
- Only seven one-line animation asset changes. No scene saved, player save modified or Build overwritten.

## Limits

Confirmed and corrected a player serialization / sampling-format mismatch. Original player visual behavior has not been rerun. An attempt to load the clip-only bundles back into this Editor was rejected by Unity with a bundle/runtime-version compatibility error despite matching displayed version; therefore no successful packed-bundle runtime sampling is claimed. Serialized bundles were inspected independently with UnityPy. The existing Build executable remains unchanged and needs rebuilding to contain this asset correction.
