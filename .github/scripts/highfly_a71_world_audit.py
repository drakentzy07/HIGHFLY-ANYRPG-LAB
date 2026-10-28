#!/usr/bin/env python3
"""HIGHFLY A7.1: audit what is actually backed by a Unity world scene.

A scene-node ScriptableObject or a CC0 prefab is NOT evidence of a playable
map. Never report placeholder nodes as installed worlds.
"""
from __future__ import annotations
import json
import re
from pathlib import Path

ROOT = Path("Assets/AnyRPG/Addons/a-lost-soul-demo-games")
SHARED = ROOT / "Content/ALostSoulDemoShared/Scenes"
NODES = ROOT / "Content/ALostSoulShared/Resources/ALostSoulShared/SceneNode"
REQUIRED = {
    "Soul Forge": "ExampleSoulForge",
    "Birch Grove": "ExampleBirchGrove",
    "Crystal Mine": "ExampleCrystalMine",
    "Lightfury Outpost": "ExampleLightfuryOutpost",
    "Mana Seal": "ExampleManaSeal",
    "Sun Temple": "ExampleSunTemple",
    "Temple of Ancient Energy": "ExampleTempleOfAncientEnergy",
    "Arena of Trials": "ExampleArenaOfTrials",
}
def field(src: str, name: str) -> str:
    match = re.search(r"^\s*" + re.escape(name) + r":\s*(.*?)\s*$", src, re.M)
    return match.group(1).strip() if match else ""

def main() -> None:
    if not SHARED.is_dir() or not NODES.is_dir():
        raise SystemExit("A7.1 missing actual Lost Soul source scenes or scene nodes")
    scenes = sorted(ROOT.rglob("*.unity"))
    scene_names = {p.stem for p in scenes}
    available = {zone: scene for zone, scene in REQUIRED.items() if scene in scene_names}
    missing = {zone: scene for zone, scene in REQUIRED.items() if scene not in scene_names}
    nodes = []
    for path in sorted(NODES.glob("*.asset")):
        src = path.read_text(encoding="utf-8")
        nodes.append({"resource_name": field(src, "resourceName"),
                      "scene_file": field(src, "sceneFile"),
                      "node_file": path.name})
    # Note: unbacked node names may describe future/legacy worlds, NOT buildable areas.
    physical = sorted(p.stem for p in scenes if p.is_relative_to(SHARED))
    game_scenes = sorted(p.stem for p in scenes if not p.is_relative_to(SHARED))
    fantasy = Path("Assets/AnyRPG/Addons/anyrpg-cc0-fantasy-content-pack")
    npc_profiles = len(list((ROOT/"Content/ALostSoulShared/Resources/ALostSoulShared/UnitProfile").rglob("*.asset")))
    quest_profiles = len(list((ROOT/"Content/ALostSoulShared/Resources/ALostSoulShared/Quest").rglob("*.asset")))
    report = {
        "purpose": "Build-time content audit only; in-game portal behavior remains unverified",
        "scene_node_count": len(nodes),
        "scene_nodes": nodes,
        "actual_shared_scene_names": physical,
        "actual_game_scene_names": game_scenes,
        "testable_world_destinations": available,
        "missing_required_world_scenes": missing,
        "unit_profile_asset_count": npc_profiles,
        "quest_asset_count": quest_profiles,
        "fantasy_pack_present": fantasy.exists(),
    }
    out=Path("world-audit")
    out.mkdir(exist_ok=True)
    (out/"a71-world-report.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    md = [
        "# HIGHFLY A7.1 — World inventory (build-time, NOT gameplay verified)",
        f"- Source scene nodes: **{len(nodes)}** (many are placeholders / unbacked).",
        f"- Original shared Unity scenes: **{len(physical)}**.",
        f"- Quests: **{quest_profiles}** asset definitions; unit profiles: **{npc_profiles}** assets.",
        f"- Fantasy pack on disk: **{fantasy.exists()}**.",
        "## Real scene-backed areas safe for isolated travel testing",
        *[f"- {zone}: `{scene}`" for zone, scene in available.items()],
        "## Missing required destination scenes",
        *([f"- {zone}: `{scene}`" for zone, scene in missing.items()] or ["- None of eight selected test scenes"]),
        "## What this test does NOT prove",
        "- Portal interaction, spawn-marker correctness, actual NPC population, quest progression, enemy balancing, save/load, and Android FPS.",
        "- The 36 nodes are NOT 36 working maps.",
    ]
    (out/"a71-world-report.md").write_text("\n".join(md)+"\n",encoding="utf-8")
    print("\n".join(md))
    if missing:
        raise SystemExit("Required playable test scenes absent: "+str(missing))
    if len(physical)<8 or len(nodes)<20:
        raise SystemExit("Unexpectedly incomplete source add-on")
if __name__ == "__main__":
    main()
