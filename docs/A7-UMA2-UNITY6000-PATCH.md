# HIGHFLY A7 — UMA v2.14f6 / Unity 6000.3.10f1 compatibility patch

## Verified failure

GitHub Actions run 40620892447 (FULL world) compiled under Unity 6000.3.10f1 and stopped with:

`Assets/UMA/Core/StandardAssets/UMA/Scripts/MeshHideAsset.cs(23,10): error CS0592: Attribute 'SerializeField' is not valid on this declaration type. It is only valid on 'field' declarations.`

Official upstream: `umasteeringgroup/UMA`, release tag `v2.14f6`, copied into `Assets/UMA` by the A7 workflow. The upstream file applies `[SerializeField]` immediately before the *property* `public SlotDataAsset asset`. Its backing field `private SlotDataAsset _asset;` is separately marked `[SerializeField]`.

## Patch scope

After the original upstream install and **before Unity starts**, the workflow runs `.github/scripts/highfly_patch_uma2_unity6.py`.

- Replaces **exactly one** illegal property-level `[SerializeField]` with a comment.
- Leaves the field-level `[SerializeField]`, getter, setter, callbacks, and all other UMA files unchanged.
- Preserves the original C# file's encoding/BOM, line endings, and Unity `.meta` GUID.
- Validates the exact code shape; an unexpected future UMA revision causes a hard failure rather than an unsafe speculative modification.
- Keeps upstream cloned source at `/tmp/highfly-upstream-uma2`; changes only the disposable CI working copy.

This is a C# compatibility fix; it is **not** evidence that the entire FULL world, every scene reference, FURY HUD, or WebGL player works. Confirm actual Unity compilation and inspect the real A Lost Soul scenes before marking A7 GREEN.

## Preservation

Do not alter the A6.2 backup branch or replace the target scene with the AnyRPG Features Demo. Keep the source's original licenses/notices. Re-check any additional compile errors individually before new patches.
