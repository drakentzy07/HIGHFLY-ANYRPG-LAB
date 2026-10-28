#!/usr/bin/env python3
"""HIGHFLY A7: minimal, reproducible UMA v2.14f6 compatibility patch for Unity 6000.

Run AFTER cloning/copying the original upstream UMA source. Keep source pristine
at /tmp/highfly-upstream-uma2 and preserve the original file's BOM and line endings.
Fails closed if the expected upstream code is absent or changed.
"""
from pathlib import Path
import hashlib

source = Path("Assets/UMA/Core/StandardAssets/UMA/Scripts/MeshHideAsset.cs")
original = source.read_bytes()
eol = b"\r\n" if b"\r\n" in original else b"\n"
needle = b"        [SerializeField]" + eol + b"        public SlotDataAsset asset" + eol
replacement = (
    b"        // HIGHFLY A7: SerializeField is valid on fields, not properties." + eol
    + b"        public SlotDataAsset asset" + eol
)
field = b"        [SerializeField]" + eol + b"        private SlotDataAsset _asset;"
if original.count(needle) != 1:
    raise SystemExit("[HIGHFLY A7 UMA PATCH] Expected one serialized property; refusing unverified UMA revision.")
if original.count(field) != 1:
    raise SystemExit("[HIGHFLY A7 UMA PATCH] Backing field missing/altered; abort to preserve UMA serialization.")
updated = original.replace(needle, replacement, 1)
if updated.count(needle) or updated.count(field) != 1:
    raise SystemExit("[HIGHFLY A7 UMA PATCH] Post-patch validation failed.")
source.write_bytes(updated)
print("[HIGHFLY A7 UMA PATCH] Patched property-only SerializeField (one change).")
print("[HIGHFLY A7 UMA PATCH] Original SHA256:", hashlib.sha256(original).hexdigest())
print("[HIGHFLY A7 UMA PATCH] Patched SHA256:", hashlib.sha256(updated).hexdigest())
