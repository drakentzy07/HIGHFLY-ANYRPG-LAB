#!/usr/bin/env python3
"""Import the OFFICIAL CC0 Fantasy Content Pack in CI, preserving Unity GUIDs.
No guesswork: only files belonging to the declared AnyRPG add-on are written.
"""
import sys, tarfile, zipfile, tempfile, shutil, json
from pathlib import Path, PurePosixPath

root=Path.cwd().resolve()
archive=Path(sys.argv[1]).resolve()
prefix="Assets/AnyRPG/Addons/anyrpg-cc0-fantasy-content-pack/"
dest=root/"Assets/AnyRPG/Addons/anyrpg-cc0-fantasy-content-pack"
if not archive.is_file() or archive.stat().st_size < 10_000_000:
    raise SystemExit("CC0 pack download missing, truncated or HTML error response")
sample=[]
written=0

def add_path(name, source, folder):
    global written
    name=name.replace("\\","/").strip()
    sample.append(name) if len(sample)<14 else None
    if not name.startswith(prefix):
        return
    rel=PurePosixPath(name[len(prefix):])
    if not rel.parts or rel.is_absolute() or ".." in rel.parts: raise ValueError(name)
    target=dest.joinpath(*rel.parts)
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfileobj(source,target.open("wb"),length=1024*1024)
    written+=1

def import_package(file):
    with tempfile.TemporaryDirectory(prefix="highfly-cc0-") as t:
        temp=Path(t)
        # A Unity .unitypackage is a gzip tar of GUID/pathname, asset and asset.meta.
        with tarfile.open(file,mode="r|gz") as src:
            for member in src:
                if not member.isfile() or member.name.split("/")[-1] not in ("pathname","asset","asset.meta"):continue
                rel=PurePosixPath(member.name)
                if rel.is_absolute() or ".." in rel.parts: raise ValueError(member.name)
                target=temp.joinpath(*rel.parts)
                target.parent.mkdir(parents=True,exist_ok=True)
                with src.extractfile(member) as inp,target.open("wb") as out:
                    shutil.copyfileobj(inp,out,length=1024*1024)
        count=0
        for fn in temp.rglob("pathname"):
            pathname=fn.read_text(encoding="utf-8",errors="replace").strip().replace("\\","/")
            payload=fn.parent/"asset"
            metadata=fn.parent/"asset.meta"
            if payload.exists():
                with payload.open("rb") as fh:add_path(pathname,fh,fn.parent)
                if metadata.exists():
                    with metadata.open("rb") as mh:add_path(pathname+".meta",mh,fn.parent)
                count+=1
        print(f"Unity package catalog entries={count}")

if zipfile.is_zipfile(archive):
    with zipfile.ZipFile(archive) as z:
        packages=[i for i in z.infolist() if i.filename.endswith(".unitypackage")]
        if packages:
            with tempfile.TemporaryDirectory(prefix="highfly-zip-") as td:
                p=Path(td)/"content.unitypackage"
                with z.open(packages[0]) as inp,p.open("wb") as out:shutil.copyfileobj(inp,out)
                import_package(p)
        else:
            for member in z.infolist():
                if member.is_dir():continue
                with z.open(member) as inp:add_path(member.filename,inp,z)
elif tarfile.is_tarfile(archive):
    import_package(archive)
else:
    raise SystemExit(f"Unsupported fantasy archive format: {archive.read_bytes()[:18]!r}")

print(json.dumps({"imported_files":written,"sample_paths":sample},indent=2))
if written < 100:
    raise SystemExit(f"CC0 addon import failed: only {written} files under {prefix}; inspect package pathname roots")
(dest/"HIGHFLY-CC0-SOURCE.txt").write_text(
    "AnyRPG CC0 Fantasy Content Pack v1, May 2026, from https://www.anyrpg.org/download/anyrpg-cc0-fantasy-content-pack/\n"
    "Licensed CC0 by AnyRPG. Imported preserving original Unity .meta GUIDs.\n",
    encoding="utf-8")
