# B01–B04 gameplay integration

Goal: replace photography presentation in HouseScene, ParkScene and safe inspection without changing item IDs, saves, layout or input framework.

Authority: user integration contract, docs/design.html cross-checked against current three-frame park workflow and asset delivery contracts. Explicit fixed-50mm/manual-focus requirement supersedes the old design snapshot's 28–70mm whitebox zoom. Four ISO options remain.

Baseline: dirty tree retained; HouseScene clean in Edit mode; Console returned zero warning/error entries. No applicable AGENTS.md found. No commits or reset. Back up scenes before authoring.

- [x] Add shared resource catalog and presentation adapter: original prefabs, B03 sampled transport clips and coupled shafts, B02 focus/aperture/iris, dynamic B04 readout.
- [x] Replace House procedural visuals while preserving interaction IDs/receivers; remove zoom mapping; bind progress/cancel/restore.
- [x] Replace Park hand instruments; retain edge-mode routing and real exposure storage.
- [x] Replace House entry visual and regenerate its inspection copy; independent inspection restores source state.
- [ ] Compile and check assets/materials, then actual Play mouse interactions in House/Park, screenshots, cancellation/recovery and disk records. Programmatic checks reported separately.
- [ ] Final diff review and delivery with unresolved items clearly identified.

Implementation: use a Resources ScriptableObject referencing original GUIDs; a visual adapter owns no gameplay state and is driven by existing controllers. Uniform wrapper scale adapts metre-size models; no animated-part stretching. Disabled source suggestion colliders stay disabled; explicit existing-ID proxies serve input. No parallel state machine/save fields. B04 content generated from actual EV/ISO, never preview textures.

Implementation saved. Acceptance partial; see QA/B01-B04-Integration/IntegrationReport.md. Native drag and full exposure/development recovery remain unverified.
