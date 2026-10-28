# HIGHFLY FULL WORLD — integration manifest (2026-10-28)

## One project, never a throwaway demo
Base: HIGHFLY A6.2 `e381889358b66e6f146c7bebdf3fce6bf01bfdde`.
Contains the original AnyRPG Core 2026 project, existing HIGHFLY mobile 360° camera/movement/skills/HUD, GitHub CI, and WebGL template.
Permanent official add-ons, pinned to known SHA via Git submodules:
- A Lost Soul Demo Games (MIT), commit 9904412ea6276d98c1ed5563937d6e015dbb2359
- AnyRPG UMA addon (MIT), commit c3ade77980cfe9b0f92f19f886a86862ea4b4478

Needed third-party assets:
- CC0 Fantasy Content Pack v1 (~996 MB from https://www.anyrpg.org/download/anyrpg-cc0-fantasy-content-pack/). Assets are not held in git and must be retrieved from author, with license preserved.
- UMA2-compatible engine, original upstream umasteeringgroup/UMA v2.14f6, loaded by full CI installer.
- Runtime dependencies from Unity Package Manager (project version 6000.3.10f1).

**Important:** The two pinned submodules alone DO NOT constitute a working full scene. Every native scene must pass import/reference verification and Unity WebGL build. **Never** publish the Features Demo build under the FULL title.

To restore sources locally: `git submodule update --init --recursive`.

## License boundaries
Only import assets with explicit redistribution license. Do not import third-party unverified or copyrighted franchise content. Preserve LICENSE inside submodules. Keep A6.2 protected.

## New goal
Game start uses A Lost Soul Character Demo (full world) and world scenes; HIGHFLY HUD and FURY auto-bootstrap in mobile, NEXUS UI next.
