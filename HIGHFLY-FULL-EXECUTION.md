# HIGHFLY FULL — execution design

## Core
A6.2 FURY, mobile HUD and save data remain in the SAME Unity project. Only the first scene and world content switch to A Lost Soul. No secondary fake demo and no overwriting A6.

## Official source sets
- [x] A Lost Soul Demo Games repository pinned as Git submodule (MIT).
- [x] AnyRPG UMA Addon pinned as Git submodule (MIT).
- [ ] CC0 Fantasy Pack v1 (~996 MB): downloaded/imported from official publisher by CI, preserving every asset GUID.
- [ ] UMA2 v2.14f6 from upstream pinned release tag: restored in CI.
- [ ] Validate one FULL WebGL build with Character Demo + Story Demo + all shared world scenes.
- [ ] Test FULL world on Samsung S23 Ultra with native FURY.
- [ ] Create new NEXUS UI on the full game, not on the Features Demo.

## Build contract
The full workflow MUST fail if CC0 or UMA2 is missing, or native world scenes are absent. It MUST NOT silently substitute A6 Features Demo. CI artifact is used first, not GitHub Pages: a full-content site may exceed the 1GB Pages limit. Optimize and host separately after an actual green build.

## License
Preserve upstream add-on MIT license notices; CC0 publisher content only; verify bundled third-party assets independently before distributing.
