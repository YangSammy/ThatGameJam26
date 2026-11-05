# Ward soft reveal preview

User contract: use the current ward concept image; automatically reveal bottom to top over time; fixed noise softens and breaks up the edge; protect the eyes. No movement-driven freeze or painted reveal-order map.

Scope: a reusable Built-in shader, material, timed preview component and independent preview scene. The concept's foreground desk/paper stays visible. The boy is baked into this preview image. This first visual prototype does not replace the existing winter gameplay state machine or save format.

Implementation defaults: 12 seconds, 1 second opening hold, 2 seconds ending hold, loop for comparison. End pose conceals eyes permanently. Procedural UV noise has no time input. Material controls expose progress, feather, noise and the face-protection ellipse. Preserve image aspect ratio using letterboxing.

- [x] Author shader and timed preview in an isolated Unity project while the shared editor is playing.
- [x] Render using Unity GPU; check warm white start, visible foreground, bottom-first reveal, protected eye pixels, monotonic reveal and repeatable frozen noise. Export actual shader animation.
- [x] Deliver shader, component, scene, material and original concept texture into Assets/Art/Environments/WardPreview with original generated metas. Document preview entry and limits.

No package, render-pipeline, shared scene or save changes. Source image: exec-d6dcc567-32e9-47b9-9c26-20e90222bca2.png.

Validation: isolated Unity 2022.3.62f3 compiled the C# and shader; D3D11/RTX4060 rendered 450 frames with no eye-region leakage. Seven additional rendering checks passed. Play Mode advanced automatically to 0.98 without input and Camera.OnRenderImage produced the expected final view. Independent read-only review found no important defects. Corrected Unity's default NPOT scaling to preserve the concept's original aspect ratio.

Ruling: keep the prototype independent from the busy shared editor and winter state machine; this request is a first effect preview. This means gameplay integration and nose-point scene switching remain separate work.
